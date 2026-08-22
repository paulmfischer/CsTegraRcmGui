namespace CsTegraRcmGui.Core.Models;

public sealed class FavoritePayload
{
    public required string Path { get; set; }

    public string DisplayName => System.IO.Path.GetFileName(Path);
}
