using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Threading;
using CsTegraRcmGui.ViewModels;

namespace CsTegraRcmGui.Views;

public partial class MainWindow : Window
{
    private ListBox? _logListBox;

    public MainWindow()
    {
        InitializeComponent();

        _logListBox = this.FindControl<ListBox>("LogListBox");
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Log.Entries.CollectionChanged += OnLogEntriesChanged;
            }
        };
    }

    private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || _logListBox is not { ItemCount: > 0 } listBox)
            return;

        // The new item's container isn't realized/measured yet at this
        // point (the ListBox is virtualized), so scrolling here can land
        // short of the true bottom. Deferring past the pending layout pass
        // lands on the actual last row.
        Dispatcher.UIThread.Post(() => listBox.ScrollIntoView(listBox.ItemCount - 1), DispatcherPriority.Background);
    }
}
