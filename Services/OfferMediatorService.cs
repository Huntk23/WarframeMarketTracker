using System;
using Microsoft.Extensions.Logging;
using WarframeMarketTracker.Models;

namespace WarframeMarketTracker.Services;

public interface IOfferMediatorService
{
    void SurfaceOffer(MarketOffer offer);
    void ClearOffer(string slug);
    void IgnoreOffer(MarketOffer offer);
    event Action<MarketOffer>? OfferAvailable;
    event Action<string>? OfferCleared;
    event Action<MarketOffer>? OrderIgnored;
}

public class OfferMediatorService : IOfferMediatorService
{
    private readonly ILogger<OfferMediatorService> _logger;

    public event Action<MarketOffer>? OfferAvailable;
    public event Action<string>? OfferCleared;
    public event Action<MarketOffer>? OrderIgnored;

    public OfferMediatorService(ILogger<OfferMediatorService> logger)
    {
        _logger = logger;
    }

    public void SurfaceOffer(MarketOffer offer) => OfferAvailable?.Invoke(offer);

    public void ClearOffer(string slug)
    {
        _logger.LogInformation("Clearing stale offer for {Slug}.", slug);
        OfferCleared?.Invoke(slug);
    }

    public void IgnoreOffer(MarketOffer offer)
    {
        _logger.LogInformation("User ignored order {OrderId} for {Slug}.", offer.OrderId, offer.Slug);
        OrderIgnored?.Invoke(offer);
        OfferCleared?.Invoke(offer.Slug);
    }
}