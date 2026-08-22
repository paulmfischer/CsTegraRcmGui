using System;
using System.Threading;
using System.Threading.Tasks;
using CegraRcmGui.Core.Services;
using CommunityToolkit.Mvvm.Input;

namespace CegraRcmGui.ViewModels;

/// <summary>
/// Mirrors the original "Tools" tab: mount SD/eMMC as USB mass storage,
/// boot Linux via ShofEL2, dump BIS keys. All three work by sending a
/// bundled payload/coreboot image to the device, so they share the same
/// device service the Payload tab uses.
/// </summary>
public partial class ToolsViewModel : ViewModelBase
{
    private readonly IRcmDeviceService _deviceService;
    private readonly LogViewModel _log;

    public ToolsViewModel(IRcmDeviceService deviceService, LogViewModel log)
    {
        _deviceService = deviceService;
        _log = log;
    }

    [RelayCommand]
    private async Task MountSdAsync() => await RunToolAsync("tools/memloader/ums_sd.scr.img", "Mounting SD card...");

    [RelayCommand]
    private async Task MountEmmcAsync() => await RunToolAsync("tools/memloader/ums_emmc.scr.img", "Mounting eMMC...");

    [RelayCommand]
    private async Task RunShofel2Async() => await RunToolAsync("shofel2/coreboot.bin", "Loading coreboot. Please wait.");

    [RelayCommand]
    private async Task DumpBisKeyAsync() => await RunToolAsync("tools/biskeydump_usb.bin", "Dumping BIS keys...");

    private async Task RunToolAsync(string toolPayloadPath, string startMessage)
    {
        _log.Log(startMessage);
        try
        {
            await _deviceService.SendPayloadAsync(toolPayloadPath, null, CancellationToken.None);
            _log.Log("Done.");
        }
        catch (Exception ex)
        {
            _log.Log($"Error: {ex.Message}");
        }
    }
}
