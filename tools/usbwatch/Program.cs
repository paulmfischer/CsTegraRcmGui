using CsTegraRcmGui.Core.Models;
using CsTegraRcmGui.Core.Services;

Console.WriteLine("Watching for a device at 0955:7321 (Ctrl+C to quit)...");

ISettingsService settings = new JsonSettingsService();
ILogger fileLogger = new FileLogger(settings);
using var deviceService = new LibUsbRcmDeviceService(fileLogger);

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
