using System.Collections.Specialized;
using Avalonia.Controls;
using CegraRcmGui.ViewModels;

namespace CegraRcmGui.Views;

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
        if (e.Action == NotifyCollectionChangedAction.Add && _logListBox is { ItemCount: > 0 } listBox)
        {
            listBox.ScrollIntoView(listBox.ItemCount - 1);
        }
    }
}
