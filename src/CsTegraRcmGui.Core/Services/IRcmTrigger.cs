using LibUsbDotNet.LibUsb;

namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Issues the RCM stack-smashing GET_STATUS control transfer that lands the
/// exploit once the wrapped payload has been written. Standard libusb
/// control transfers of this size are rejected locally by both Linux's and
/// Windows' backends before ever reaching the device, so each OS needs its
/// own raw workaround; other platforms fall back to the standard libusb
/// control transfer, which does work there.
/// </summary>
public interface IRcmTrigger
{
    bool Trigger(IUsbDevice device, int vendorId, int productId, int triggerLength, ILogger log);
}
