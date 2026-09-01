using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CsTegraRcmGui.Core.Services;
using CsTegraRcmGui.Services;
using CsTegraRcmGui.ViewModels;
using CsTegraRcmGui.Views;

namespace CsTegraRcmGui;

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
            var log = new LogViewModel();
            ILogger logger = new CompositeLogger(new FileLogger(), log);
            var deviceService = new LibUsbRcmDeviceService(logger);

            var mainViewModel = new MainViewModel(
                new PayloadViewModel(deviceService, favorites, logger),
                new ToolsViewModel(deviceService, logger),
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
