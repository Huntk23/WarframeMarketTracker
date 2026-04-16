using System.Text.Json.Serialization;

namespace WarframeMarketTracker.Models;

public record WfmUser(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("ingameName")] string IngameName,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("platform")] string Platform,
    [property: JsonPropertyName("crossplay")] bool Crossplay,
    [property: JsonPropertyName("avatar")] string? Avatar = null,
    [property: JsonPropertyName("reputation")] int? Reputation = null,
    [property: JsonPropertyName("locale")] string? Locale = null,
    [property: JsonPropertyName("activity")] WfmUserActivity? Activity = null,
    [property: JsonPropertyName("lastSeen")] string? LastSeen = null
);

public record WfmUserActivity(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("details")] string? Details = null,
    [property: JsonPropertyName("startedAt")] string? StartedAt = null
);