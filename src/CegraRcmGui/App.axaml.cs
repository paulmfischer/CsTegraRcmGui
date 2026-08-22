using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CegraRcmGui.Core.Services;
using CegraRcmGui.ViewModels;
using CegraRcmGui.Views;

namespace CegraRcmGui;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ISettingsService settings = new JsonSettingsService();
            IFavoritesService favorites = new FavoritesService(settings);
            var deviceService = new LibUsbRcmDeviceService();
            var log = new LogViewModel();

            var mainViewModel = new MainViewModel(
                new PayloadViewModel(deviceService, favorites, log),
                new ToolsViewModel(deviceService, log),
                new OptionsViewModel(settings),
                log);

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };
            desktop.ShutdownRequested += (_, _) => deviceService.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
