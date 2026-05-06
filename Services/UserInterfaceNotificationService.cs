using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Microsoft.Extensions.Logging;
using WarframeMarketTracker.Models;

namespace WarframeMarketTracker.Services;

public interface IUserInterfaceNotificationService
{
    void SurfaceOffer(MarketOffer offer);
    void ClearOffer(string slug);
    void IgnoreOffer(string orderId);
    Task CopyWhisperAsync(string whisper);
    event Action<MarketOffer>? OfferAvailable;
    event Action<string>? OfferCleared;
    event Action<string>? OrderIgnored;
}

public class UserInterfaceNotificationService : IUserInterfaceNotificationService
{
    private readonly ILogger<UserInterfaceNotificationService> _logger;

    public event Action<MarketOffer>? OfferAvailable;
    public event Action<string>? OfferCleared;
    public event Action<string>? OrderIgnored;

    public UserInterfaceNotificationService(ILogger<UserInterfaceNotificationService> logger)
    {
        _logger = logger;
    }

    public void SurfaceOffer(MarketOffer offer) => OfferAvailable?.Invoke(offer);

    public void ClearOffer(string slug)
    {
        _logger.LogInformation("Clearing stale offer for {Slug}.", slug);
        OfferCleared?.Invoke(slug);
    }

    public void IgnoreOffer(string orderId)
    {
        _logger.LogInformation("User ignored order {OrderId}.", orderId);
        OrderIgnored?.Invoke(orderId);
    }

    public async Task CopyWhisperAsync(string whisper)
    {
        var clipboard = GetClipboard();
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

    private static IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is { } window)
        {
            return TopLevel.GetTopLevel(window)?.Clipboard;
        }

        return null;
    }
}