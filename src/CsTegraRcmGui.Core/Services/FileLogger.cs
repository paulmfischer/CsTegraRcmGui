namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Writes every log level to a file next to the app.
/// </summary>
public sealed class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public FileLogger(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "cstegrarcmgui.log");
    }

    public void Debug(string message) => Write($"DEBUG: {message}");

    public void Info(string message) => Write($"INFO: {message}");

    public void Error(string context, Exception ex) => Write($"ERROR: {context}: {ex}");

    private void Write(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";

        lock (_lock)
        {
            File.AppendAllText(_filePath, line);
        }
    }
}
