using System;
using CegraRcmGui.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CegraRcmGui.ViewModels;

public partial class OptionsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    public partial bool AutoInject { get; set; }

    [ObservableProperty]
    public partial bool LoggingEnabled { get; set; }

    public OptionsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        var settings = _settingsService.Current;
        AutoInject = settings.AutoInject;
        LoggingEnabled = settings.LoggingEnabled;
    }

    partial void OnAutoInjectChanged(bool value) => Persist(s => s.AutoInject = value);

    partial void OnLoggingEnabledChanged(bool value) => Persist(s => s.LoggingEnabled = value);

    private void Persist(Action<Core.Models.AppSettings> apply)
    {
        apply(_settingsService.Current);
        _settingsService.Save();
    }
}
