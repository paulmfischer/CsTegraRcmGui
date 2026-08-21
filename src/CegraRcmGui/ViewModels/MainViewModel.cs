using CegraRcmGui.Core.Services;

namespace CegraRcmGui.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public PayloadViewModel Payload { get; }
    public ToolsViewModel Tools { get; }
    public OptionsViewModel Options { get; }

    public MainViewModel(PayloadViewModel payload, ToolsViewModel tools, OptionsViewModel options)
    {
        Payload = payload;
        Tools = tools;
        Options = options;
    }

    /// <summary>Parameterless constructor for the XAML designer only.</summary>
    public MainViewModel() : this(DesignTime.Payload, DesignTime.Tools, DesignTime.Options)
    {
    }

    private static class DesignTime
    {
        private static readonly ISettingsService Settings = new JsonSettingsService(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CegraRcmGui-designer"));
        private static readonly IFavoritesService FavoritesService = new FavoritesService(Settings);
        private static readonly IRcmDeviceService DeviceService = new StubRcmDeviceService();

        public static PayloadViewModel Payload => new(DeviceService, FavoritesService);
        public static ToolsViewModel Tools => new(DeviceService);
        public static OptionsViewModel Options => new(Settings);
    }
}
