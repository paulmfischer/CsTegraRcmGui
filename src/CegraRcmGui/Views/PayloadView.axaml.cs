using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CegraRcmGui.ViewModels;

namespace CegraRcmGui.Views;

public partial class PayloadView : UserControl
{
    public PayloadView()
    {
        InitializeComponent();
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PayloadViewModel viewModel)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a payload",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Binary payload") { Patterns = ["*.bin"] },
                new FilePickerFileType("All files") { Patterns = ["*"] },
            ],
        });

        if (files.Count > 0)
        {
            viewModel.SelectedPayloadPath = files[0].Path.LocalPath;
        }
    }
}
