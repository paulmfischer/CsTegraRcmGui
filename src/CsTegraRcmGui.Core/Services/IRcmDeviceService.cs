using CsTegraRcmGui.Core.Models;

namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Talks to a Tegra device sitting in USB recovery mode: detects it and
/// transfers a payload to it. The original app did this over libusbK
/// (Windows-only); this interface is the seam where a cross-platform
/// libusb-backed implementation gets plugged in.
/// </summary>
public interface IRcmDeviceService
{
    /// <summary>Raised whenever the device's connection state changes.</summary>
    event EventHandler<RcmDeviceState>? StateChanged;

    RcmDeviceState GetState();

    /// <summary>Blocks (asynchronously) until a device appears, or cancellation is requested.</summary>
    Task<RcmDeviceState> WaitForDeviceAsync(CancellationToken cancellationToken);

    Task SendPayloadAsync(string payloadPath, IProgress<string>? progress, CancellationToken cancellationToken);
}
