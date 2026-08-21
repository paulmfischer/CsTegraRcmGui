using CegraRcmGui.Core.Models;

namespace CegraRcmGui.Core.Services;

public sealed class FavoritesService : IFavoritesService
{
    private readonly ISettingsService _settings;
    private readonly List<FavoritePayload> _favorites;

    public IReadOnlyList<FavoritePayload> Favorites => _favorites;

    public FavoritesService(ISettingsService settings)
    {
        _settings = settings;
        _favorites = _settings.Current.Favorites
            .Select(path => new FavoritePayload { Path = path })
            .ToList();
    }

    public bool Add(string payloadPath)
    {
        if (_favorites.Any(f => f.Path == payloadPath))
            return false;

        _favorites.Add(new FavoritePayload { Path = payloadPath });
        Persist();
        return true;
    }

    public bool Remove(string payloadPath)
    {
        var removed = _favorites.RemoveAll(f => f.Path == payloadPath) > 0;
        if (removed)
            Persist();

        return removed;
    }

    private void Persist()
    {
        _settings.Current.Favorites = _favorites.Select(f => f.Path).ToList();
        _settings.Save();
    }
}
