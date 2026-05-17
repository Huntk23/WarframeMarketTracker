using System;

namespace WarframeMarketTracker.Models;

public static class MarketUrls
{
    public static Uri SaleLink(string slug) => new($"https://warframe.market/items/{slug}?type=sell");
}