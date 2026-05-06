using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    bool TryGetByName(string name, [NotNullWhen(true)] out ItemShort? item);

    Task InitializeAsync(CancellationToken ct);
}

public class ItemCache : IItemCache
{
    private readonly HttpClient _httpClient;
    private FrozenDictionary<string, ItemShort>? _lookup;

    // Materialized once at hydration; the catalog is immutable for the app's lifetime, so callers
    // get a stable, pre-sorted snapshot without re-allocating on every binding read.
    public IReadOnlyList<ItemShort> Items { get; private set; } = [];

    public int Count => Items.Count;

    public ItemCache(IHttpClientFactory httpClientFactory) => _httpClient = httpClientFactory.CreateClient("WfmApi");

    public bool TryGetByName(string name, [NotNullWhen(true)] out ItemShort? item)
    {
        if (_lookup is not null && _lookup.TryGetValue(name, out var found))
        {
            item = found;
            return true;
        }
        item = null;
        return false;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        var response = await _httpClient.GetFromJsonAsync<WfmResponse<List<ItemShort>>>("items", ct);
        if (response?.Data == null) return;

        _lookup = response.Data.ToFrozenDictionary(i => i.EnglishName, StringComparer.OrdinalIgnoreCase);
        Items = response.Data.OrderBy(i => i.EnglishName).ToArray();
    }
}