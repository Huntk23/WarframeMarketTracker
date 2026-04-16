using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Labs.Notifications;
using Avalonia.Threading;
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

    // Pending notification data keyed by notification ID
    private readonly Dictionary<uint, PendingNotification> _pending = new();

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

    public Task ShowNotificationAsync(string title, string body, string whisper, string orderId)
    {
        var manager = NativeNotificationManager.Current;
        if (manager == null) return Task.CompletedTask;

        var notification = manager.CreateNotification("market_alerts");
        if (notification == null) return Task.CompletedTask;

        notification.Title = title;
        notification.Message = body;
        notification.SetActions(NotificationActions);

        _pending[notification.Id] = new PendingNotification(whisper, orderId);

        notification.Show();

        return Task.CompletedTask;
    }

    private void OnNotificationCompleted(object? sender, NativeNotificationCompletedEventArgs e)
    {
        if (!e.NotificationId.HasValue || !_pending.TryGetValue(e.NotificationId.Value, out var data))
            return;

        _pending.Remove(e.NotificationId.Value);

        switch (e.ActionTag)
        {
            case CopyActionTag:
                Dispatcher.UIThread.Post(async () =>
                {
                    var clipboard = GetClipboard();
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(data.Whisper);
                        _logger.LogInformation("Whisper copied to clipboard.");
                    }
                    else
                    {
                        _logger.LogWarning("Clipboard unavailable.");
                    }
                });
                break;
            case IgnoreActionTag:
                _logger.LogInformation("User ignored order {OrderId}.", data.OrderId);
                OrderIgnored?.Invoke(data.OrderId);
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

    private record PendingNotification(string Whisper, string OrderId);
}