using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace CegraRcmGui.ViewModels;

/// <summary>Accumulates timestamped log entries for the lifetime of the app.</summary>
public partial class LogViewModel : ViewModelBase
{
    public ObservableCollection<string> Entries { get; } = [];

    public void Log(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";

        if (Dispatcher.UIThread.CheckAccess())
        {
            Entries.Add(entry);
        }
        else
        {
            Dispatcher.UIThread.Post(() => Entries.Add(entry));
        }
    }
}
