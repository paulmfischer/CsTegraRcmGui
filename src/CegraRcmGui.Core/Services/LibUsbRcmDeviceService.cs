using CegraRcmGui.Core.Models;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace CegraRcmGui.Core.Services;

/// <summary>
/// Talks to a Tegra X1 device sitting in its USB recovery-mode (RCM)
/// bootloader over libusb, via LibUsbDotNet. Replaces the original app's
/// Windows-only libusbk-based implementation (TegraRcmSmash.cpp's
/// RCMDeviceHacker class), which this is a direct port of.
///
/// Sequence: read the 16-byte device id the bootloader sends on connect,
/// stream the payload to the device in fixed-size packets (the device
/// alternates which of two fixed IRAM addresses each packet lands at),
/// then issue one GET_STATUS control transfer sized to reach from the
/// last write position to the end of the device's stack. The bootloader's
/// copy of that request overruns its own stack, landing on the payload.
/// </summary>
public sealed class LibUsbRcmDeviceService : IRcmDeviceService, IDisposable
{
    private const int VendorId = 0x0955;
    private const int ProductId = 0x7321;
    private const int PacketSize = 0x1000;
    private const uint LowBufferAddress = 0x40005000;
    private const uint HighBufferAddress = 0x40009000;
    private const uint StackEndAddress = 0x40010000;
    private const int TransferTimeoutMs = 3000;
    private const int PollIntervalMs = 500;

    private readonly UsbContext _context = new();
    private readonly CancellationTokenSource _monitorCts = new();
    private RcmDeviceState _lastState = RcmDeviceState.NotConnected;

    public event EventHandler<RcmDeviceState>? StateChanged;

    public LibUsbRcmDeviceService()
    {
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
        var payload = await File.ReadAllBytesAsync(payloadPath, cancellationToken);

        var device = FindDevice()
            ?? throw new InvalidOperationException("No device found in USB recovery mode.");

        try
        {
            device.Open();
            device.ClaimInterface(0);

            var reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
            var writer = device.OpenEndpointWriter(WriteEndpointID.Ep01);

            progress?.Report("Reading device id...");
            var deviceId = new byte[0x10];
            reader.Read(deviceId, TransferTimeoutMs, out _);

            progress?.Report("Writing payload...");
            var lastPacketTargetedHighAddress = WritePayload(writer, payload, cancellationToken);

            if (!lastPacketTargetedHighAddress)
            {
                // Layout requires the last packet before the trigger to have
                // landed at the high address; pad with one empty packet if it didn't.
                writer.Write(new byte[PacketSize], TransferTimeoutMs, out _).ThrowOnError();
            }

            progress?.Report("Triggering payload execution...");
            TriggerExecution(device);
            progress?.Report("Payload injected!");
        }
        finally
        {
            // Once the trigger lands, the device has jumped into the payload
            // and is gone from the bus, so closing the now-stale handle is
            // expected to fail. That failure doesn't mean the injection did.
            try
            {
                device.Dispose();
            }
            catch (UsbException)
            {
            }
        }
    }

    private static bool WritePayload(UsbEndpointWriter writer, byte[] payload, CancellationToken cancellationToken)
    {
        var targetsHighAddressNext = false;
        var offset = 0;
        while (offset < payload.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunkLength = Math.Min(PacketSize, payload.Length - offset);
            writer.Write(payload.AsSpan(offset, chunkLength), TransferTimeoutMs, out _).ThrowOnError();

            offset += chunkLength;
            targetsHighAddressNext = !targetsHighAddressNext;
        }

        return targetsHighAddressNext;
    }

    private static void TriggerExecution(IUsbDevice device)
    {
        var triggerLength = (int)(StackEndAddress - HighBufferAddress);
        var setupPacket = new UsbSetupPacket(
            bRequestType: 0x82, // Device-to-host | Standard | Recipient=Endpoint
            bRequest: 0x00,     // GET_STATUS
            wValue: 0,
            wIndex: 0,
            wlength: triggerLength);

        try
        {
            device.ControlTransfer(setupPacket, new byte[triggerLength], 0, triggerLength);
        }
        catch (UsbException)
        {
            // Expected: once the trigger lands, the device jumps into the
            // payload and stops responding on this pipe.
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
