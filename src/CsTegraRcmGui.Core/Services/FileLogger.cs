namespace CsTegraRcmGui.Core.Services;

public sealed class FileLogger : IFileLogger
{
    private readonly ISettingsService _settings;
    private readonly string _filePath;
    private readonly object _lock = new();

    public FileLogger(ISettingsService settings, string? filePath = null)
    {
        _settings = settings;
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "cstegrarcmgui.log");
    }

    public void Log(string message)
    {
        if (!_settings.Current.LoggingEnabled)
            return;

        Write($"INFO: {message}");
    }

    public void LogError(string context, Exception ex)
    {
        if (!_settings.Current.LoggingEnabled)
            return;

        Write($"ERROR: {context}: {ex}");
    }

    private void Write(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";

        lock (_lock)
        {
            File.AppendAllText(_filePath, line);
        }
    }
}
