using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WarframeMarketTracker.Models;

namespace WarframeMarketTracker.Services;

public interface IItemCache
{
    IReadOnlyList<ItemShort> Items { get; }

    int Count { get; }

    Task InitializeAsync(CancellationToken ct);
}

public class ItemCache : IItemCache
{
    private readonly HttpClient _httpClient;
    private FrozenDictionary<string, ItemShort>? _cache;

    public IReadOnlyList<ItemShort> Items => _cache?.Values.ToList() ?? [];

    public int Count => Items.Count;

    public ItemCache(HttpClient httpClient) => _httpClient = httpClient;

    public async Task InitializeAsync(CancellationToken ct)
    {
        var response = await _httpClient.GetFromJsonAsync<WfmResponse<List<ItemShort>>>("items", ct);
        if (response?.Data != null)
        {
            _cache = response.Data.ToFrozenDictionary(i => i.EnglishName, StringComparer.OrdinalIgnoreCase);
        }
    }
}