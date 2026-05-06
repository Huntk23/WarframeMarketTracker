using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly INotificationService _toast;
    private readonly IUserInterfaceNotificationService _uiNotificationService;
    private readonly ILogger<MarketPollingService> _logger;

    // Tracks the last price we notified about per slug — only re-notify if a lower price appears
    private readonly Dictionary<string, int> _lastNotifiedPrice = new();

    // Tracks the order ID we last surfaced to the UI per slug — used to detect when that specific deal disappears
    // (seller went offline, item sold) so we can clear the label.
    private readonly Dictionary<string, string> _lastNotifiedOfferId = new();

    // Orders the user chose to ignore for this session
    private readonly HashSet<string> _ignoredOrderIds = new();

    public MarketPollingService(
        IWarframeMarketService api,
        ITrackedItemRegistry registry,
        INotificationService toast,
        IUserInterfaceNotificationService uiNotificationService,
        ILogger<MarketPollingService> logger)
    {
        _api = api;
        _registry = registry;
        _toast = toast;
        _uiNotificationService = uiNotificationService;
        _logger = logger;

        _uiNotificationService.OrderIgnored += orderId => _ignoredOrderIds.Add(orderId);
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
                    var orders = await _api.GetOrdersBySlugAsync(item.Slug, stoppingToken);
                    if (item.TargetRank.HasValue)
                        orders = orders.Where(o => o.Rank == item.TargetRank.Value).ToList();

                    var lowestOrder = orders.FirstOrDefault();
                    var hasDeal = lowestOrder != null && lowestOrder.Platinum <= item.TargetPlatinum;
                    var isIgnored = hasDeal && _ignoredOrderIds.Contains(lowestOrder!.Id);

                    if (hasDeal && !isIgnored)
                    {
                        var isNewLowerPrice = !_lastNotifiedPrice.TryGetValue(item.Slug, out var lastPrice)
                                              || lowestOrder!.Platinum < lastPrice;

                        if (isNewLowerPrice)
                        {
                            _lastNotifiedPrice[item.Slug] = lowestOrder!.Platinum;
                            _lastNotifiedOfferId[item.Slug] = lowestOrder.Id;

                            LogDealFoundWithTargetPrice(item.ItemName, lowestOrder.Platinum, item.TargetPlatinum);

                            var offer = new MarketOffer(
                                item.Slug,
                                item.ItemName,
                                lowestOrder.Id,
                                lowestOrder.Platinum,
                                item.TargetPlatinum,
                                lowestOrder.User.IngameName,
                                lowestOrder.GenerateWhisper(item.ItemName));

                            // Surface to UI first so labels appear even if the OS toast fails
                            _uiNotificationService.SurfaceOffer(offer);
                            await _toast.ShowOfferAsync(offer);
                        }
                        else if (_lastNotifiedOfferId.TryGetValue(item.Slug, out var lastId)
                                 && lastId != lowestOrder!.Id)
                        {
                            // Same/higher price than last notification, but a different seller is now cheapest - the original deal is gone.
                            LogDealCleared(item.ItemName);
                            
                            _lastNotifiedOfferId.Remove(item.Slug);
                            _lastNotifiedPrice.Remove(item.Slug);
                            _uiNotificationService.ClearOffer(item.Slug);
                        }
                    }
                    else
                    {
                        // Either no qualifying deal exists, or the cheapest is the deal the user ignored.
                        _lastNotifiedPrice.Remove(item.Slug);
                        
                        if (_lastNotifiedOfferId.Remove(item.Slug))
                        {
                            LogDealCleared(item.ItemName);
                            
                            _uiNotificationService.ClearOffer(item.Slug);
                        }
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

    [LoggerMessage(LogLevel.Information, "[DEAL CLEARED] {ItemName} - previously surfaced offer is no longer available.")]
    partial void LogDealCleared(string itemName);
}