using System.Text.Json.Serialization;

namespace WarframeMarketTracker.Models;

public record SavedTrackedItem(
    [property: JsonPropertyName("itemName")] string ItemName,
    [property: JsonPropertyName("targetPlatinum")] int TargetPlatinum,
    [property: JsonPropertyName("targetRank")] int? TargetRank,
    [property: JsonPropertyName("isEnabled")] bool IsEnabled
);