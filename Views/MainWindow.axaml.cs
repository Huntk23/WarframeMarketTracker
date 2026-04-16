using Avalonia.Controls;
using Avalonia.Interactivity;
using WarframeMarketTracker.ViewModels;

namespace WarframeMarketTracker.Views;

public partial class MainWindow : ShadUI.Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Hide to tray instead of closing
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }

    private void AutoCompleteBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is AutoCompleteBox box && box.DataContext is TrackedItemViewModel vm)
        {
            // If the text in the box doesn't exactly match the selected item
            // or any item in the source, clear it.
            if (box.SelectedItem == null)
            {
                box.Text = string.Empty;
                vm.ItemName = string.Empty;
            }
        }
    }
}