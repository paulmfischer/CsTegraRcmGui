using System;
using CsTegraRcmGui.Core.Services;
using CommunityToolkit.Mvvm.Input;

namespace CsTegraRcmGui.ViewModels;

public partial class OptionsViewModel : ViewModelBase
{
    private readonly IFolderOpener _folderOpener;
    private readonly ILogger _log;

    public string LogFilePath { get; }

    public OptionsViewModel(string logFilePath, IFolderOpener folderOpener, ILogger log)
    {
        LogFilePath = logFilePath;
        _folderOpener = folderOpener;
        _log = log;
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        try
        {
            _folderOpener.OpenContainingFolder(LogFilePath);
        }
        catch (Exception ex)
        {
            _log.Error("Failed to open log folder", ex);
        }
    }
}
