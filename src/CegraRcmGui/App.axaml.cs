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
            IRcmDeviceService deviceService = new StubRcmDeviceService();

            var mainViewModel = new MainViewModel(
                new PayloadViewModel(deviceService, favorites),
                new ToolsViewModel(deviceService),
                new OptionsViewModel(settings));

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
