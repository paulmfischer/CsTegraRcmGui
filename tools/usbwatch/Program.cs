using CegraRcmGui.Core.Models;
using CegraRcmGui.Core.Services;

Console.WriteLine("Watching for a device at 0955:7321 (Ctrl+C to quit)...");

using var deviceService = new LibUsbRcmDeviceService();

RcmDeviceState? lastState = null;
while (true)
{
    var state = deviceService.GetState();
    if (state != lastState)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] State: {state}");
        lastState = state;
    }

    Thread.Sleep(500);
}
