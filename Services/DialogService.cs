using System;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Microsoft.Extensions.Logging;
using WarframeMarketTracker.ViewModels;
using WarframeMarketTracker.Views;

namespace WarframeMarketTracker.Services;

public interface IDialogService
{
    Task ShowAboutAsync();
    Task CopyWhisperAsync(string whisper);
}

public class DialogService : IDialogService
{
    private readonly Lazy<MainWindowViewModel> _mainViewModel;
    private readonly Func<AboutWindowViewModel> _aboutViewModelFactory;
    private readonly ILogger<DialogService> _logger;

    public DialogService(
        Lazy<MainWindowViewModel> mainViewModel,
        Func<AboutWindowViewModel> aboutViewModelFactory,
        ILogger<DialogService> logger)
    {
        _mainViewModel = mainViewModel;
        _aboutViewModelFactory = aboutViewModelFactory;
        _logger = logger;
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

    public async Task CopyWhisperAsync(string whisper)
    {
        var clipboard = _mainViewModel.Value.Owner?.Clipboard;
        if (clipboard == null)
        {
            _logger.LogWarning("Clipboard unavailable.");
            return;
        }

        try
        {
            await clipboard.SetTextAsync(whisper);
            _logger.LogInformation("Whisper copied to clipboard.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to copy whisper to clipboard.");
        }
    }
}