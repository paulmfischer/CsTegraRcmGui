using CegraRcmGui.Core.Models;

namespace CegraRcmGui.Core.Services;

/// <summary>
/// Placeholder used while the UI is built out. Reports "not connected" and
/// throws on send, so callers/UI wiring can be developed and tested before
/// the real libusb-backed <see cref="IRcmDeviceService"/> implementation lands.
/// </summary>
public sealed class StubRcmDeviceService : IRcmDeviceService
{
    public event EventHandler<RcmDeviceState>? StateChanged;

    public RcmDeviceState GetState() => RcmDeviceState.NotConnected;

    public Task<RcmDeviceState> WaitForDeviceAsync(CancellationToken cancellationToken)
    {
        StateChanged?.Invoke(this, RcmDeviceState.NotConnected);
        return Task.FromResult(RcmDeviceState.NotConnected);
    }

    public Task SendPayloadAsync(string payloadPath, IProgress<string>? progress, CancellationToken cancellationToken)
        => throw new NotSupportedException("USB device communication is not yet implemented.");
}
