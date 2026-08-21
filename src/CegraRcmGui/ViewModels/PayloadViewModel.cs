using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CegraRcmGui.Core.Models;
using CegraRcmGui.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CegraRcmGui.ViewModels;

public partial class PayloadViewModel : ViewModelBase
{
    private readonly IRcmDeviceService _deviceService;
    private readonly IFavoritesService _favoritesService;

    [ObservableProperty]
    public partial string? SelectedPayloadPath { get; set; }

    [ObservableProperty]
    public partial RcmDeviceState DeviceState { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Waiting for device in RCM mode";

    public ObservableCollection<FavoritePayload> Favorites { get; } = [];

    public PayloadViewModel(IRcmDeviceService deviceService, IFavoritesService favoritesService)
    {
        _deviceService = deviceService;
        _favoritesService = favoritesService;
        _deviceService.StateChanged += (_, state) => DeviceState = state;

        foreach (var favorite in _favoritesService.Favorites)
            Favorites.Add(favorite);
    }

    [RelayCommand(CanExecute = nameof(CanInject))]
    private async Task InjectAsync()
    {
        if (SelectedPayloadPath is null)
            return;

        StatusMessage = "Injecting payload...";
        try
        {
            await _deviceService.SendPayloadAsync(SelectedPayloadPath, null, CancellationToken.None);
            StatusMessage = "Payload injected!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error while injecting payload: {ex.Message}";
        }
    }

    private bool CanInject() => !string.IsNullOrEmpty(SelectedPayloadPath);

    [RelayCommand]
    private void AddFavorite()
    {
        if (SelectedPayloadPath is null || !_favoritesService.Add(SelectedPayloadPath))
            return;

        Favorites.Add(_favoritesService.Favorites[^1]);
    }

    [RelayCommand]
    private void SelectFavorite(FavoritePayload favorite) => SelectedPayloadPath = favorite.Path;

    [RelayCommand]
    private void RemoveFavorite(FavoritePayload favorite)
    {
        if (!_favoritesService.Remove(favorite.Path))
            return;

        Favorites.Remove(favorite);
    }

    partial void OnSelectedPayloadPathChanged(string? value) => InjectCommand.NotifyCanExecuteChanged();
}
