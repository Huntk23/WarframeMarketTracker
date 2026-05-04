using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Labs.Notifications;
using Microsoft.Extensions.Logging;

namespace WarframeMarketTracker.Services;

public class NativeNotificationService : INotificationService
{
    private const string CopyActionTag = "copy";
    private const string IgnoreActionTag = "ignore";

    private static readonly NativeNotificationAction[] NotificationActions =
    [
        new("Copy Whisper", CopyActionTag),
        new("Ignore Offer", IgnoreActionTag)
    ];

    private readonly ILogger<NativeNotificationService> _logger;
    private readonly Dictionary<uint, MarketOffer> _pending = new();

    public event Action<MarketOffer>? OfferAvailable;
    public event Action<string>? OrderIgnored;

    public NativeNotificationService(ILogger<NativeNotificationService> logger)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        var manager = NativeNotificationManager.Current;
        if (manager != null)
        {
            manager.NotificationCompleted += OnNotificationCompleted;
            _logger.LogInformation("Avalonia Labs Notification Manager registered.");
        }
    }

    public Task NotifyOfferAsync(MarketOffer offer)
    {
        // Always raise the in-app event so the UI can show a fallback even if the toast doesn't fire
        OfferAvailable?.Invoke(offer);

        var manager = NativeNotificationManager.Current;
        if (manager == null) return Task.CompletedTask;

        var notification = manager.CreateNotification(Program.NotificationChannelId);
        if (notification == null) return Task.CompletedTask;

        notification.Title = $"Deal Found: {offer.ItemName}";
        notification.Message =
            $"{offer.Platinum}p from {offer.SellerName}{Environment.NewLine}Target: {offer.TargetPlatinum}p";
        notification.SetActions(NotificationActions);

        _pending[notification.Id] = offer;
        notification.Show();

        return Task.CompletedTask;
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

    private void OnNotificationCompleted(object? sender, NativeNotificationCompletedEventArgs e)
    {
        if (!e.NotificationId.HasValue || !_pending.TryGetValue(e.NotificationId.Value, out var offer))
            return;

        _pending.Remove(e.NotificationId.Value);

        switch (e.ActionTag)
        {
            case CopyActionTag:
                _ = CopyWhisperAsync(offer.Whisper);
                break;
            case IgnoreActionTag:
                IgnoreOffer(offer.OrderId);
                break;
            default:
                var url = $"https://warframe.market/items/{offer.Slug}?type=sell";
                OpenUrl(url);
                _logger.LogInformation("Opened sale page for {Slug}.", offer.Slug);
                break;
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

    private static void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Process.Start("xdg-open", url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
    }
}