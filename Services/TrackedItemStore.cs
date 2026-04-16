using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WarframeMarketTracker.Models;

namespace WarframeMarketTracker.Services;

public interface ITrackedItemStore
{
    List<SavedTrackedItem> Load();
    void Save(IEnumerable<SavedTrackedItem> items);
}

public class TrackedItemStore : ITrackedItemStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly ILogger<TrackedItemStore> _logger;

    public TrackedItemStore(ILogger<TrackedItemStore> logger)
    {
        _logger = logger;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "WarframeMarketTracker");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "tracked-items.json");
    }

    public List<SavedTrackedItem> Load()
    {
        if (!File.Exists(_filePath))
            return [];

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<SavedTrackedItem>>(json, JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tracked items from {Path}", _filePath);
            return [];
        }
    }

    public void Save(IEnumerable<SavedTrackedItem> items)
    {
        try
        {
            var json = JsonSerializer.Serialize(items, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save tracked items to {Path}", _filePath);
        }
    }
}
