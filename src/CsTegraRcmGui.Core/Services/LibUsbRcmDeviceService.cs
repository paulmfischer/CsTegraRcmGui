using CsTegraRcmGui.Core.Models;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Talks to a Tegra X1 device sitting in its USB recovery-mode (RCM)
/// bootloader over libusb, via LibUsbDotNet. Replaces the original app's
/// Windows-only libusbk-based implementation (TegraRcmSmash.cpp's
/// RCMDeviceHacker class), which only covered raw USB transport — the
/// payload-wrapping logic below (<see cref="BuildRcmPayload"/>) lived in
/// that app's separate, unavailable TegraRcmSmash.exe, so it's ported here
/// from the equivalent, independently-verified-working logic in JTegraNX
/// (github.com/dylwedma11748/JTegraNX, src/main/java/rcm/RCM.java).
///
/// Sequence: read the 16-byte device id the bootloader sends on connect,
/// wrap the raw payload into the layout the bootloader's RCM handler
/// expects (an init sequence, a small ARM relocator stub, the payload
/// itself, and a spray of a jump-target pointer that fills the region the
/// stack overflow below will land in), stream it to the device in fixed
/// packets, then issue one GET_STATUS control transfer sized to overrun the
/// bootloader's stack and jump into the relocator stub.
/// </summary>
public sealed class LibUsbRcmDeviceService : IRcmDeviceService, IDisposable
{
    private const int VendorId = 0x0955;
    private const int ProductId = 0x7321;
    private const int PacketSize = 0x1000;
    private const uint HighBufferAddress = 0x40009000;
    private const uint StackEndAddress = 0x40010000;
    private const int TransferTimeoutMs = 3000;
    private const int PollIntervalMs = 500;
    private const int MaxTransferAttempts = 3;
    private const int RetryDelayMs = 200;

    // RCM payload wrapper layout — see BuildRcmPayload.
    private const int MinRawPayloadSize = 0x4000;
    private const int MaxRawPayloadSize = 0x1ed58;
    private const int MaxWrappedPayloadSize = 0x30298;
    private const int IntermezzoOffset = 0x2a8;
    private const int PayloadHeadOffset = 0x10e8;
    private const int PayloadHeadLength = 0x4000;
    private const int SpreadOffset = 0x50e8;
    private const int SpreadIterations = 0x870;
    private const int PayloadTailOffset = 0x72a8;

    private static readonly byte[] InitSequence = [0x98, 0x02, 0x03];
    private static readonly byte[] SpreadPattern = [0x00, 0x00, 0x01, 0x40];

    // Small ARM relocator stub that runs first: copies the real payload
    // into place and jumps to it. Landed on by the stack-smashing trigger.
    private static readonly byte[] Intermezzo =
    [
        0x5c, 0x00, 0x9f, 0xe5, 0x5c, 0x10, 0x9f, 0xe5, 0x5c, 0x20, 0x9f, 0xe5, 0x01, 0x20, 0x42, 0xe0,
        0x0e, 0x00, 0x00, 0xeb, 0x48, 0x00, 0x9f, 0xe5, 0x10, 0xff, 0x2f, 0xe1, 0x00, 0x00, 0xa0, 0xe1,
        0x48, 0x00, 0x9f, 0xe5, 0x48, 0x10, 0x9f, 0xe5, 0x01, 0x29, 0xa0, 0xe3, 0x07, 0x00, 0x00, 0xeb,
        0x38, 0x00, 0x9f, 0xe5, 0x01, 0x19, 0xa0, 0xe3, 0x01, 0x00, 0x80, 0xe0, 0x34, 0x10, 0x9f, 0xe5,
        0x03, 0x28, 0xa0, 0xe3, 0x01, 0x00, 0x00, 0xeb, 0x20, 0x00, 0x9f, 0xe5, 0x10, 0xff, 0x2f, 0xe1,
        0x04, 0x30, 0x91, 0xe4, 0x04, 0x30, 0x80, 0xe4, 0x04, 0x20, 0x52, 0xe2, 0xfb, 0xff, 0xff, 0x1a,
        0x1e, 0xff, 0x2f, 0xe1, 0x00, 0xf0, 0x00, 0x40, 0x20, 0x00, 0x01, 0x40, 0x7c, 0x00, 0x01, 0x40,
        0x00, 0x00, 0x01, 0x40, 0x40, 0x0e, 0x01, 0x40, 0x00, 0x70, 0x01, 0x40,
    ];

    private readonly UsbContext _context = new();
    private readonly CancellationTokenSource _monitorCts = new();
    private readonly ILogger _log;
    private RcmDeviceState _lastState = RcmDeviceState.NotConnected;

    public event EventHandler<RcmDeviceState>? StateChanged;

    public LibUsbRcmDeviceService(ILogger log)
    {
        _log = log;
        _ = MonitorAsync(_monitorCts.Token);
    }

    public RcmDeviceState GetState()
    {
        using var device = FindDevice();
        return device is null ? RcmDeviceState.NotConnected : RcmDeviceState.Connected;
    }

    public async Task<RcmDeviceState> WaitForDeviceAsync(CancellationToken cancellationToken)
    {
        RcmDeviceState state;
        while ((state = GetState()) != RcmDeviceState.Connected)
        {
            await Task.Delay(PollIntervalMs, cancellationToken);
        }

        return state;
    }

    private async Task MonitorAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var state = GetState();
            if (state != _lastState)
            {
                _lastState = state;
                StateChanged?.Invoke(this, state);
            }

            try
            {
                await Task.Delay(PollIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task SendPayloadAsync(string payloadPath, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var rawPayload = await File.ReadAllBytesAsync(payloadPath, cancellationToken);
        _log.Debug($"SendPayloadAsync: '{payloadPath}' ({rawPayload.Length} bytes)");

        var payload = BuildRcmPayload(rawPayload);
        _log.Debug($"Built RCM payload: {payload.Length} bytes ({payload.Length / PacketSize} blocks)");

        IUsbDevice? device = null;
        try
        {
            device = FindDevice()
                ?? throw new InvalidOperationException("No device found in USB recovery mode.");

            device.Open();
            _log.Debug($"Opened device VID={device.VendorId:X4} PID={device.ProductId:X4}");
            device.ClaimInterface(0);
            _log.Debug("Claimed interface 0");

            var reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
            var writer = device.OpenEndpointWriter(WriteEndpointID.Ep01);

            progress?.Report("Reading device id...");
            var deviceId = new byte[0x10];
            var (readError, readLength) = await TransferWithRetryAsync(
                "Read device id", reader, () => reader.ReadAsync(deviceId, TransferTimeoutMs), cancellationToken);
            if (readLength > 0)
                _log.Debug($"Device id bytes: {Convert.ToHexString(deviceId.AsSpan(0, readLength))}");
            readError.ThrowOnError();

            progress?.Report("Writing payload...");
            await WriteRcmPayloadAsync(writer, payload, cancellationToken);

            progress?.Report("Triggering payload execution...");
            var triggerLength = (int)(StackEndAddress - HighBufferAddress);
            if (OperatingSystem.IsLinux() && device is UsbDevice concreteDevice)
            {
                // Standard libusb control transfers of this size are
                // rejected locally (Error.InvalidParam) on Linux without
                // ever reaching the device — a single 0x7000-byte usbfs URB
                // apparently isn't accepted through that path. Submit it as
                // a raw usbfs URB instead, the same workaround JTegraNX's
                // own Linux-specific native helper uses.
                if (!LinuxRcmTrigger.Trigger(concreteDevice.BusNumber, concreteDevice.Address, triggerLength, _log))
                    throw new InvalidOperationException("Trigger transfer did not land — the device is still alive and did not jump to the payload.");
            }
            else
            {
                TriggerExecution(device, triggerLength, _log);
            }
            progress?.Report("Payload injected!");
        }
        catch (Exception ex)
        {
            _log.Error("SendPayloadAsync failed", ex);
            throw;
        }
        finally
        {
            // Once the trigger lands, the device has jumped into the payload
            // and is gone from the bus, so closing the now-stale handle is
            // expected to fail. That failure doesn't mean the injection did.
            try
            {
                device?.Dispose();
                _log.Debug("Closed device handle");
            }
            catch (UsbException ex)
            {
                _log.Debug($"Closing device handle failed (expected once the trigger has landed): {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Wraps a raw payload binary into the layout the bootloader's RCM
    /// handler expects: an init sequence, the relocator stub, the first
    /// 0x4000 bytes of the payload, a spray of a pointer to the stack-end
    /// address (so the overrun below lands on it no matter exactly where it
    /// hits), then the rest of the payload. The total size is padded up to
    /// a packet-size boundary and nudged to an odd number of packets, which
    /// is what makes the final packet land at the high IRAM buffer.
    /// </summary>
    private static byte[] BuildRcmPayload(byte[] rawPayload)
    {
        if (rawPayload.Length < MinRawPayloadSize || rawPayload.Length > MaxRawPayloadSize)
        {
            throw new InvalidOperationException(
                $"Payload size {rawPayload.Length} bytes is outside the valid range ({MinRawPayloadSize}-{MaxRawPayloadSize} bytes).");
        }

        var totalSize = PayloadHeadOffset + rawPayload.Length + SpreadIterations * SpreadPattern.Length;
        totalSize += PacketSize - totalSize % PacketSize;
        if (totalSize / PacketSize % 2 == 0)
            totalSize += PacketSize;

        if (totalSize > MaxWrappedPayloadSize)
        {
            throw new InvalidOperationException(
                $"Wrapped payload size {totalSize} bytes exceeds the device's maximum of {MaxWrappedPayloadSize} bytes.");
        }

        var wrapped = new byte[totalSize];
        InitSequence.CopyTo(wrapped, 0);
        Intermezzo.CopyTo(wrapped, IntermezzoOffset);
        Array.Copy(rawPayload, 0, wrapped, PayloadHeadOffset, PayloadHeadLength);

        for (var i = 0; i < SpreadIterations; i++)
            SpreadPattern.CopyTo(wrapped, SpreadOffset + i * SpreadPattern.Length);

        Array.Copy(rawPayload, PayloadHeadLength, wrapped, PayloadTailOffset, rawPayload.Length - PayloadHeadLength);

        return wrapped;
    }

    private async Task WriteRcmPayloadAsync(UsbEndpointWriter writer, byte[] payload, CancellationToken cancellationToken)
    {
        var blockCount = payload.Length / PacketSize;
        for (var i = 0; i < blockCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var offset = i * PacketSize;
            var (error, _) = await TransferWithRetryAsync(
                $"Write block {i + 1}/{blockCount}",
                writer,
                () => writer.WriteAsync(payload.AsMemory(offset, PacketSize), TransferTimeoutMs),
                cancellationToken);
            error.ThrowOnError();
        }
    }

    private static void TriggerExecution(IUsbDevice device, int triggerLength, ILogger log)
    {
        var setupPacket = new UsbSetupPacket(
            bRequestType: 0x82, // Device-to-host | Standard | Recipient=Endpoint
            bRequest: 0x00,     // GET_STATUS
            wValue: 0,
            wIndex: 0,
            wlength: triggerLength);

        log.Debug($"Sending trigger control transfer: length={triggerLength}");
        try
        {
            var transferred = device.ControlTransfer(setupPacket, new byte[triggerLength], 0, triggerLength);
            log.Debug($"Trigger control transfer completed without error: {transferred} bytes transferred (unexpected — normally the device jumps away before responding)");
        }
        catch (UsbException ex)
        {
            // Expected: once the trigger lands, the device jumps into the
            // payload and stops responding on this pipe.
            log.Debug($"Trigger control transfer threw (expected once the trigger lands): {ex.Message}");
        }
    }

    private async Task<(Error error, int transferred)> TransferWithRetryAsync(
        string context, UsbEndpointBase endpoint, Func<Task<(Error error, int transferLength)>> transfer, CancellationToken cancellationToken)
    {
        Error error;
        int transferred;
        var attempt = 1;
        while (true)
        {
            (error, transferred) = await transfer();
            _log.Debug($"{context} (attempt {attempt}/{MaxTransferAttempts}): {error}, {transferred} bytes");

            if (error == Error.Success || attempt >= MaxTransferAttempts)
                return (error, transferred);

            // A failed transfer (timeout or otherwise) commonly leaves the
            // endpoint halted/stalled, which makes every subsequent attempt
            // fail immediately with a pipe error until it's cleared.
            var clearHaltError = endpoint.ClearHalt();
            _log.Debug($"{context}: clearing halt before retry: {clearHaltError}");

            await Task.Delay(RetryDelayMs, cancellationToken);
            attempt++;
        }
    }

    private IUsbDevice? FindDevice() =>
        _context.Find(d => d.VendorId == VendorId && d.ProductId == ProductId);

    public void Dispose()
    {
        _monitorCts.Cancel();
        _monitorCts.Dispose();
        _context.Dispose();
    }
}
