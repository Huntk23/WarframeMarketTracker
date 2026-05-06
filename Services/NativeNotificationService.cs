using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Labs.Notifications;
using Microsoft.Extensions.Logging;
using WarframeMarketTracker.Models;

namespace WarframeMarketTracker.Services;

public interface INotificationService
{
    void Initialize();
    Task ShowOfferAsync(MarketOffer offer);
}

public class NativeNotificationService : INotificationService
{
    private const string ChannelId = "market_alerts";
    private const string AppUserModelId = "com.warframe.market.tracker";
    private const string CopyActionTag = "copy";
    private const string IgnoreActionTag = "ignore";

    private static readonly NotificationChannel[] Channels =
    [
        new(ChannelId, "Warframe Market Alerts", NotificationPriority.High)
    ];

    private static readonly NativeNotificationAction[] NotificationActions =
    [
        new("Copy Whisper", CopyActionTag),
        new("Ignore Offer", IgnoreActionTag)
    ];

    public static readonly AppNotificationOptions AppNotificationOptions = new()
    {
        Channels = Channels,
        AppName = Program.AppName,
        // Required for Windows Toast notifications to work correctly
        AppUserModelId = AppUserModelId,
    };

    private readonly ILogger<NativeNotificationService> _logger;
    private readonly IUserInterfaceNotificationService _uiNotificationService;
    private readonly Dictionary<uint, MarketOffer> _pendingOffers = new();

    public NativeNotificationService(
        ILogger<NativeNotificationService> logger,
        IUserInterfaceNotificationService uiNotificationService)
    {
        _logger = logger;
        _uiNotificationService = uiNotificationService;
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

    public Task ShowOfferAsync(MarketOffer offer)
    {
        var manager = NativeNotificationManager.Current;
        if (manager == null) return Task.CompletedTask;

        var notification = manager.CreateNotification(ChannelId);
        if (notification == null) return Task.CompletedTask;

        notification.Title = $"Deal Found: {offer.ItemName}";
        notification.Message =
            $"{offer.Platinum}p from {offer.SellerName}{Environment.NewLine}Target: {offer.TargetPlatinum}p";
        notification.SetActions(NotificationActions);

        _pendingOffers[notification.Id] = offer;
        notification.Show();

        return Task.CompletedTask;
    }

    private void OnNotificationCompleted(object? sender, NativeNotificationCompletedEventArgs e)
    {
        if (!e.NotificationId.HasValue || !_pendingOffers.TryGetValue(e.NotificationId.Value, out var offer))
            return;

        _pendingOffers.Remove(e.NotificationId.Value);

        switch (e.ActionTag)
        {
            case CopyActionTag:
                _ = _uiNotificationService.CopyWhisperAsync(offer.Whisper);
                break;
            case IgnoreActionTag:
                _uiNotificationService.IgnoreOffer(offer.OrderId);
                break;
            default:
                var url = $"https://warframe.market/items/{offer.Slug}?type=sell";
                OpenUrl(url);
                _logger.LogInformation("Opened sale page for {Slug}.", offer.Slug);
                break;
        }
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