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
            var fileLogger = new FileLogger(
                maxSizeBytes: settings.Current.LogMaxSizeBytes,
                retainedFileCount: settings.Current.LogRetainedFileCount);
            ILogger logger = new CompositeLogger(fileLogger, log);
            IFolderOpener folderOpener = PlatformServices.CreateFolderOpener();
            var deviceService = new LibUsbRcmDeviceService(logger, PlatformServices.CreateRcmTrigger());

            var mainViewModel = new MainViewModel(
                new PayloadViewModel(deviceService, favorites, logger),
                new ToolsViewModel(deviceService, logger),
                new OptionsViewModel(fileLogger.FilePath, folderOpener, logger),
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
