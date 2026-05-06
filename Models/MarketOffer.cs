namespace WarframeMarketTracker.Models;

public record MarketOffer(
    string Slug,
    string ItemName,
    string OrderId,
    int Platinum,
    int TargetPlatinum,
    string SellerName,
    string Whisper);