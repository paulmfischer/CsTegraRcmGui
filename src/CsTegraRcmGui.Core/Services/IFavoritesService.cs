using CsTegraRcmGui.Core.Models;

namespace CsTegraRcmGui.Core.Services;

public interface IFavoritesService
{
    IReadOnlyList<FavoritePayload> Favorites { get; }

    bool Add(string payloadPath);

    bool Remove(string payloadPath);
}
