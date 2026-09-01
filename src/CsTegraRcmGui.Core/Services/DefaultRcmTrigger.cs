using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Fallback for platforms without a raw-transport workaround (see
/// <see cref="LinuxRcmTrigger"/>/<see cref="WindowsRcmTrigger"/>): issues
/// the trigger as a standard libusb control transfer.
/// </summary>
internal sealed class DefaultRcmTrigger : IRcmTrigger
{
    /// <summary>
    /// Returns true only when the transfer failed in a way consistent with
    /// the device having jumped away mid-transfer. A completed transfer, or
    /// one rejected with <see cref="Error.InvalidParam"/> — which on
    /// Windows means WinUSB's 4KB control-transfer ceiling rejected this
    /// call locally, before it ever reached the device — both mean the
    /// trigger did not land.
    /// </summary>
    public bool Trigger(IUsbDevice device, int vendorId, int productId, int triggerLength, ILogger log)
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
            return false;
        }
        catch (UsbException ex) when (ex.ErrorCode == Error.InvalidParam)
        {
            // Rejected locally by the USB stack (e.g. WinUSB's hard 4KB
            // control-transfer limit on Windows) — never reached the
            // device, so the trigger did not land.
            log.Debug($"Trigger control transfer rejected locally, never reached the device: {ex.Message}");
            return false;
        }
        catch (UsbException ex)
        {
            // Expected: once the trigger lands, the device jumps into the
            // payload and stops responding on this pipe.
            log.Debug($"Trigger control transfer threw (expected once the trigger lands): {ex.Message}");
            return true;
        }
    }
}
