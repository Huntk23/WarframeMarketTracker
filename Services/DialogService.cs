using System;
using System.Threading.Tasks;
using WarframeMarketTracker.ViewModels;
using WarframeMarketTracker.Views;

namespace WarframeMarketTracker.Services;

public interface IDialogService
{
    Task ShowAboutAsync();
}

public class DialogService : IDialogService
{
    private readonly Lazy<MainWindowViewModel> _mainViewModel;
    private readonly Func<AboutWindowViewModel> _aboutViewModelFactory;

    public DialogService(
        Lazy<MainWindowViewModel> mainViewModel,
        Func<AboutWindowViewModel> aboutViewModelFactory)
    {
        _mainViewModel = mainViewModel;
        _aboutViewModelFactory = aboutViewModelFactory;
    }

    public async Task ShowAboutAsync()
    {
        var owner = _mainViewModel.Value.Owner;
        if (owner == null) return;

        var viewModel = _aboutViewModelFactory();
        var window = new AboutWindow { DataContext = viewModel };
        _ = viewModel.CheckForUpdateAsync();
        await window.ShowDialog(owner);
    }
}
