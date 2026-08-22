using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CegraRcmGui.Core.Models;
using CegraRcmGui.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CegraRcmGui.ViewModels;

public partial class PayloadViewModel : ViewModelBase
{
    private readonly IRcmDeviceService _deviceService;
    private readonly IFavoritesService _favoritesService;
    private readonly LogViewModel _log;

    [ObservableProperty]
    public partial string? SelectedPayloadPath { get; set; }

    [ObservableProperty]
    public partial RcmDeviceState DeviceState { get; set; }

    public ObservableCollection<FavoritePayload> Favorites { get; } = [];

    public PayloadViewModel(IRcmDeviceService deviceService, IFavoritesService favoritesService, LogViewModel log)
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

        _log.Log($"Injecting payload: {SelectedPayloadPath}");
        try
        {
            await _deviceService.SendPayloadAsync(SelectedPayloadPath, null, CancellationToken.None);
            _log.Log("Payload injected.");
        }
        catch (Exception ex)
        {
            _log.Log($"Error while injecting payload: {ex.Message}");
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
        _log.Log(ConnectionStatusText);
    }

    [RelayCommand]
    private void AddFavorite()
    {
        if (SelectedPayloadPath is null || !_favoritesService.Add(SelectedPayloadPath))
            return;

        Favorites.Add(_favoritesService.Favorites[^1]);
        _log.Log($"Added favorite: {SelectedPayloadPath}");
    }

    [RelayCommand]
    private void SelectFavorite(FavoritePayload favorite) => SelectedPayloadPath = favorite.Path;

    [RelayCommand]
    private void RemoveFavorite(FavoritePayload favorite)
    {
        if (!_favoritesService.Remove(favorite.Path))
            return;

        Favorites.Remove(favorite);
        _log.Log($"Removed favorite: {favorite.Path}");
    }

    partial void OnSelectedPayloadPathChanged(string? value) => InjectCommand.NotifyCanExecuteChanged();
}
