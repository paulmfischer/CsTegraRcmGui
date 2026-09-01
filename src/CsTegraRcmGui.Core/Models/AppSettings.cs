namespace CsTegraRcmGui.Core.Models;

public sealed class AppSettings
{
    public List<string> Favorites { get; set; } = [];
    public long LogMaxSizeBytes { get; set; } = 5 * 1024 * 1024;
    public int LogRetainedFileCount { get; set; } = 2;
}
