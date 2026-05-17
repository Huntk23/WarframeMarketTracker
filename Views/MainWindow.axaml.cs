using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WarframeMarketTracker.ViewModels;

namespace WarframeMarketTracker.Views;

public partial class MainWindow : ShadUI.Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Owner = this;
        viewModel.RowAdded += OnRowAdded;
    }

    private void OnRowAdded(TrackedItemViewModel vm)
    {
        // Defer until after layout so the new container has been realized
        Dispatcher.UIThread.Post(() =>
        {
            var container = TrackedItemsControl.ContainerFromItem(vm);
            container?.BringIntoView();

            var autoComplete = container?.GetVisualDescendants().OfType<AutoCompleteBox>().FirstOrDefault();
            autoComplete?.Focus();
        }, DispatcherPriority.Loaded);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Hide to tray instead of closing
        if (e.CloseReason == WindowCloseReason.WindowClosing)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnClosing(e);
    }

    private void AutoCompleteBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is AutoCompleteBox box && box.DataContext is TrackedItemViewModel vm)
        {
            // SelectedItem is only set when the user picks from the dropdown — typed-but-not-picked names and saved-state loads leave it null.
            // IsValid is the authoritative signal that the current text resolves to a real item, so use that to decide whether to clear.
            if (!vm.IsValid)
            {
                box.Text = string.Empty;
                vm.ItemName = string.Empty;
            }
        }
    }
}