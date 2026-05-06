using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WarframeMarketTracker.Models;

namespace WarframeMarketTracker.Services;

public interface IWarframeMarketService
{
    Task<List<OrderWithUser>> GetOrdersBySlugAsync(string slug, CancellationToken ct = default);
}

public class WarframeMarketService : IWarframeMarketService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WarframeMarketService> _logger;

    public WarframeMarketService(IHttpClientFactory httpClientFactory, ILogger<WarframeMarketService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("WfmApi");
        _logger = logger;
    }

    public async Task<List<OrderWithUser>> GetOrdersBySlugAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<WfmResponse<List<OrderWithUser>>>(
                $"orders/item/{slug}", ct);

            if (response?.Data == null) return [];

            // Filtering for sellers who are actually available to trade
            return response.Data
                .Where(o => o.Type == "sell" && o.User.Status == "ingame")
                .OrderBy(o => o.Platinum)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch orders for slug: {Slug}", slug);
            return [];
        }
    }

}