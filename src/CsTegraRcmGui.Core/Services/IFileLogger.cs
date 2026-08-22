namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Writes diagnostic detail to a log file next to the app (USB transfer
/// results, exception detail) that's too noisy for the in-app log panel.
/// Gated live by <see cref="Models.AppSettings.LoggingEnabled"/> — mirrors
/// the original app's "Enable logging" option.
/// </summary>
public interface IFileLogger
{
    void Log(string message);

    void LogError(string context, Exception ex);
}
