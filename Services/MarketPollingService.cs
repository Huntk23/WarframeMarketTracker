using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WarframeMarketTracker.Models;

namespace WarframeMarketTracker.Services;

public partial class MarketPollingService : BackgroundService
{
    private readonly IWarframeMarketService _api;
    private readonly ITrackedItemRegistry _registry;
    private readonly INotificationService _notifications;
    private readonly ILogger<MarketPollingService> _logger;

    // Tracks the last price we notified about per slug — only re-notify if a lower price appears
    private readonly Dictionary<string, int> _lastNotifiedPrice = new();

    // Orders the user chose to ignore for this session
    private readonly HashSet<string> _ignoredOrderIds = new();

    public MarketPollingService(
        IWarframeMarketService api,
        ITrackedItemRegistry registry,
        INotificationService notifications,
        ILogger<MarketPollingService> logger)
    {
        _api = api;
        _registry = registry;
        _notifications = notifications;
        _logger = logger;

        _notifications.OrderIgnored += orderId => _ignoredOrderIds.Add(orderId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 10 seconds before the first poll to let the UI boot up
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        _logger.LogInformation("Market Poller Started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var activeItems = _registry.GetActiveItems();

            // Wait before each poll cycle
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            if (activeItems.Count == 0)
            {
                _logger.LogDebug("No active items tracking. Skipping poll cycle.");
                continue;
            }

            LogActiveItemsBeingPolled(activeItems.Count, activeItems);

            foreach (var item in activeItems)
            {
                try
                {
                    var lowestOrder = await _api.GetLowestSellOrderAsync(item.Slug, item.TargetRank, stoppingToken);

                    if (lowestOrder != null && lowestOrder.Platinum <= item.TargetPlatinum)
                    {
                        // Skip orders the user has chosen to ignore this session
                        if (_ignoredOrderIds.Contains(lowestOrder.Id))
                            continue;

                        // Skip if we already notified at this price or lower
                        if (_lastNotifiedPrice.TryGetValue(item.Slug, out var lastPrice)
                            && lowestOrder.Platinum >= lastPrice)
                        {
                            continue;
                        }

                        _lastNotifiedPrice[item.Slug] = lowestOrder.Platinum;

                        LogDealFoundWithTargetPrice(item.ItemName, lowestOrder.Platinum, item.TargetPlatinum);

                        var whisper = lowestOrder.GenerateWhisper(item.ItemName);

                        await _notifications.ShowNotificationAsync(
                            $"Deal Found: {item.ItemName}",
                            $"{lowestOrder.Platinum}p from {lowestOrder.User.IngameName} {Environment.NewLine}Target: {item.TargetPlatinum}p",
                            whisper,
                            lowestOrder.Id);
                    }
                    else
                    {
                        // Price rose above target or no orders — reset so we re-notify if it drops again
                        _lastNotifiedPrice.Remove(item.Slug);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to poll data for {ItemName}", item.ItemName);
                }

                // 500ms between requests (API limit: 3/sec, we stay well under at 2/sec)
                await Task.Delay(500, stoppingToken);
            }
        }
    }

    [LoggerMessage(LogLevel.Debug, "Polling Market for {Count} active items: {@ActiveItems}")]
    partial void LogActiveItemsBeingPolled(int count, IReadOnlyList<TrackedItemEntry> activeItems);

    [LoggerMessage(LogLevel.Information, "[DEAL FOUND] {ItemName} is selling for {Platinum}p (Target: {Target}p).")]
    partial void LogDealFoundWithTargetPrice(string itemName, int platinum, int target);
}