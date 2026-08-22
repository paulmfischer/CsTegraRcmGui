using System;
using CsTegraRcmGui.Core.Services;
using CsTegraRcmGui.ViewModels;

namespace CsTegraRcmGui.Services;

/// <summary>
/// Fans a single log call out to both sinks: everything goes to the file
/// log, but only Info/Error — the events a user would care about — also
/// go to the in-app log panel. Debug is diagnostic detail meant for
/// troubleshooting from the file, not for the UI.
/// </summary>
public sealed class CompositeLogger(ILogger fileLog, LogViewModel uiLog) : ILogger
{
    public void Debug(string message) => fileLog.Debug(message);

    public void Info(string message)
    {
        fileLog.Info(message);
        uiLog.Log(message);
    }

    public void Error(string context, Exception ex)
    {
        fileLog.Error(context, ex);
        uiLog.Log($"{context}: {ex.Message}");
    }
}
