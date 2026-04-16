using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WarframeMarketTracker.Models;

public record WfmResponse<T>(
    [property: JsonPropertyName("apiVersion")] string ApiVersion,
    [property: JsonPropertyName("data")] T Data,
    [property: JsonPropertyName("error")] object? Error
);

public record ItemShort(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("gameRef")] string GameRef,
    [property: JsonPropertyName("tags")] string[] Tags,
    [property: JsonPropertyName("i18n")] Dictionary<string, ItemI18N> I18N,
    [property: JsonPropertyName("maxRank")] int? MaxRank,
    [property: JsonPropertyName("vaulted")] bool? Vaulted,
    [property: JsonPropertyName("subtypes")] string[]? Subtypes
)
{
    public string EnglishName => I18N.TryGetValue("en", out var en) ? en.Name : Slug;
}

public record ItemI18N(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("icon")] string Icon,
    [property: JsonPropertyName("thumb")] string Thumb
);