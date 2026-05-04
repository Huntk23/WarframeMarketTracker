using System;
using System.Threading.Tasks;

namespace WarframeMarketTracker.Services;

public record MarketOffer(
    string Slug,
    string ItemName,
    string OrderId,
    int Platinum,
    int TargetPlatinum,
    string SellerName,
    string Whisper);

public interface INotificationService
{
    Task NotifyOfferAsync(MarketOffer offer);
    void IgnoreOffer(string orderId);
    Task CopyWhisperAsync(string whisper);
    event Action<MarketOffer>? OfferAvailable;
    event Action<string>? OrderIgnored;
    void Initialize();
}