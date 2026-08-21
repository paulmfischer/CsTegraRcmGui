namespace CegraRcmGui.Core.Models;

public sealed class AppSettings
{
    public bool AutoInject { get; set; }
    public bool LoggingEnabled { get; set; }
    public List<string> Favorites { get; set; } = [];
}
