using CegraRcmGui.Core.Models;

namespace CegraRcmGui.Core.Services;

public interface IFavoritesService
{
    IReadOnlyList<FavoritePayload> Favorites { get; }

    bool Add(string payloadPath);

    bool Remove(string payloadPath);
}
