namespace CsTegraRcmGui.Core.Models;

public sealed class AppSettings
{
    public bool AutoInject { get; set; }
    public List<string> Favorites { get; set; } = [];
}
