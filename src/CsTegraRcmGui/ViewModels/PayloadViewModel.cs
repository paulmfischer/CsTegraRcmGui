using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CsTegraRcmGui.Core.Models;
using CsTegraRcmGui.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CsTegraRcmGui.ViewModels;

public partial class PayloadViewModel : ViewModelBase
{
    private readonly IRcmDeviceService _deviceService;
    private readonly IFavoritesService _favoritesService;
    private readonly ILogger _log;

    [ObservableProperty]
    public partial string? SelectedPayloadPath { get; set; }

    [ObservableProperty]
    public partial RcmDeviceState DeviceState { get; set; }

    public ObservableCollection<FavoritePayload> Favorites { get; } = [];

    public PayloadViewModel(IRcmDeviceService deviceService, IFavoritesService favoritesService, ILogger log)
    {
        _deviceService = deviceService;
        _favoritesService = favoritesService;
        _log = log;
        _deviceService.StateChanged += (_, state) => Dispatcher.UIThread.Post(() => DeviceState = state);
        DeviceState = _deviceService.GetState();

        foreach (var favorite in _favoritesService.Favorites)
            Favorites.Add(favorite);
    }

    [RelayCommand(CanExecute = nameof(CanInject))]
    private async Task InjectAsync()
    {
        if (SelectedPayloadPath is null)
            return;

        _log.Info($"Injecting payload: {SelectedPayloadPath}");
        try
        {
            await _deviceService.SendPayloadAsync(SelectedPayloadPath, null, CancellationToken.None);
            _log.Info("Payload injected.");
        }
        catch (Exception)
        {
            // Already logged (with full detail) by the device service.
        }
    }

    private bool CanInject() => !string.IsNullOrEmpty(SelectedPayloadPath) && DeviceState == RcmDeviceState.Connected;

    public string ConnectionStatusText => DeviceState switch
    {
        RcmDeviceState.Connected => "Device connected",
        RcmDeviceState.WrongDriver => "Device found, but the driver isn't set up correctly",
        RcmDeviceState.Error => "Device error",
        _ => "Waiting for device in RCM mode...",
    };

    partial void OnDeviceStateChanged(RcmDeviceState value)
    {
        InjectCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ConnectionStatusText));
        _log.Info(ConnectionStatusText);
    }

    [RelayCommand]
    private void AddFavorite()
    {
        if (SelectedPayloadPath is null || !_favoritesService.Add(SelectedPayloadPath))
            return;

        Favorites.Add(_favoritesService.Favorites[^1]);
        _log.Info($"Added favorite: {SelectedPayloadPath}");
    }

    [RelayCommand]
    private void SelectFavorite(FavoritePayload favorite) => SelectedPayloadPath = favorite.Path;

    [RelayCommand]
    private void RemoveFavorite(FavoritePayload favorite)
    {
        if (!_favoritesService.Remove(favorite.Path))
            return;

        Favorites.Remove(favorite);
        _log.Info($"Removed favorite: {favorite.Path}");
    }

    partial void OnSelectedPayloadPathChanged(string? value) => InjectCommand.NotifyCanExecuteChanged();
}
