namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// App-wide logging sink. <see cref="Debug"/> is for noisy diagnostic
/// detail (USB transfer results, per-attempt retries) that's only useful
/// when digging into a specific failure; <see cref="Info"/> and
/// <see cref="Error"/> are user-meaningful events. A given implementation
/// decides which levels it surfaces — see <see cref="FileLogger"/> (all
/// three) and the UI-facing composite that also forwards Info/Error to the
/// in-app log panel.
/// </summary>
public interface ILogger
{
    void Debug(string message);

    void Info(string message);

    void Error(string context, Exception ex);
}
