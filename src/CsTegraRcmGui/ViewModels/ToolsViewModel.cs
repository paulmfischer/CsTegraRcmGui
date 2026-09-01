using System;
using System.Threading;
using System.Threading.Tasks;
using CsTegraRcmGui.Core.Services;
using CommunityToolkit.Mvvm.Input;

namespace CsTegraRcmGui.ViewModels;

/// <summary>
/// Mirrors part of the original "Tools" tab: mount SD/eMMC as USB mass
/// storage. Both work by sending a bundled memloader payload to the device,
/// so they share the same device service the Payload tab uses.
/// </summary>
public partial class ToolsViewModel : ViewModelBase
{
    private readonly IRcmDeviceService _deviceService;
    private readonly ILogger _log;

    public ToolsViewModel(IRcmDeviceService deviceService, ILogger log)
    {
        _deviceService = deviceService;
        _log = log;
    }

    [RelayCommand]
    private async Task MountSdAsync() => await RunToolAsync("tools/memloader/ums_sd.scr.img", "Mounting SD card...");

    [RelayCommand]
    private async Task MountEmmcAsync() => await RunToolAsync("tools/memloader/ums_emmc.scr.img", "Mounting eMMC...");

    private async Task RunToolAsync(string toolPayloadPath, string startMessage)
    {
        _log.Info(startMessage);
        try
        {
            await _deviceService.SendPayloadAsync(toolPayloadPath, null, CancellationToken.None);
            _log.Info("Done.");
        }
        catch (Exception)
        {
            // Already logged (with full detail) by the device service.
        }
    }
}
