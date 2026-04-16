using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace WarframeMarketTracker.Services;

public record TrackedItemEntry(string Slug, string ItemName, int TargetPlatinum, int? TargetRank);

public interface ITrackedItemRegistry
{
    void Register(string key, TrackedItemEntry entry);
    void Unregister(string key);
    IReadOnlyList<TrackedItemEntry> GetActiveItems();
}

public class TrackedItemRegistry : ITrackedItemRegistry
{
    private readonly ConcurrentDictionary<string, TrackedItemEntry> _items = new();

    public void Register(string key, TrackedItemEntry entry) => _items[key] = entry;

    public void Unregister(string key) => _items.TryRemove(key, out _);

    public IReadOnlyList<TrackedItemEntry> GetActiveItems() => _items.Values.ToList();
}