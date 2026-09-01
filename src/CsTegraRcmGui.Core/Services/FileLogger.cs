namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Writes every log level to a file next to the app. On construction, rotates
/// the existing file (to <c>.1</c>, <c>.2</c>, ... up to <paramref name="retainedFileCount"/>)
/// if it has grown past <paramref name="maxSizeBytes"/>.
/// </summary>
public sealed class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public string FilePath => _filePath;

    public FileLogger(string? filePath = null, long maxSizeBytes = 5 * 1024 * 1024, int retainedFileCount = 2)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "cstegrarcmgui.log");
        RotateIfNeeded(maxSizeBytes, retainedFileCount);
    }

    private void RotateIfNeeded(long maxSizeBytes, int retainedFileCount)
    {
        var info = new FileInfo(_filePath);
        if (!info.Exists || info.Length < maxSizeBytes)
            return;

        if (retainedFileCount <= 0)
        {
            File.Delete(_filePath);
            return;
        }

        var oldest = $"{_filePath}.{retainedFileCount}";
        if (File.Exists(oldest))
            File.Delete(oldest);

        for (var i = retainedFileCount - 1; i >= 1; i--)
        {
            var src = $"{_filePath}.{i}";
            if (File.Exists(src))
                File.Move(src, $"{_filePath}.{i + 1}");
        }

        File.Move(_filePath, $"{_filePath}.1");
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
