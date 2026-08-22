namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Writes every log level to a file next to the app. Gated live by
/// <see cref="Models.AppSettings.LoggingEnabled"/> — mirrors the original
/// app's "Enable logging" option.
/// </summary>
public sealed class FileLogger : ILogger
{
    private readonly ISettingsService _settings;
    private readonly string _filePath;
    private readonly object _lock = new();

    public FileLogger(ISettingsService settings, string? filePath = null)
    {
        _settings = settings;
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "cstegrarcmgui.log");
    }

    public void Debug(string message) => Write($"DEBUG: {message}");

    public void Info(string message) => Write($"INFO: {message}");

    public void Error(string context, Exception ex) => Write($"ERROR: {context}: {ex}");

    private void Write(string message)
    {
        if (!_settings.Current.LoggingEnabled)
            return;

        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";

        lock (_lock)
        {
            File.AppendAllText(_filePath, line);
        }
    }
}
