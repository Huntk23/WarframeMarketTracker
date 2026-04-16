using System.Text.Json.Serialization;

namespace WarframeMarketTracker.Models;

public record OrderWithUser(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("platinum")] int Platinum,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("rank")] int? Rank,
    [property: JsonPropertyName("itemId")] string ItemId,
    [property: JsonPropertyName("user")] WfmUser User,
    [property: JsonPropertyName("perTrade")] int? PerTrade = null,
    [property: JsonPropertyName("charges")] int? Charges = null,
    [property: JsonPropertyName("subtype")] string? Subtype = null,
    [property: JsonPropertyName("amberStars")] int? AmberStars = null,
    [property: JsonPropertyName("cyanStars")] int? CyanStars = null,
    [property: JsonPropertyName("vosfor")] int? Vosfor = null,
    [property: JsonPropertyName("visible")] bool? Visible = null,
    [property: JsonPropertyName("createdAt")] string? CreatedAt = null,
    [property: JsonPropertyName("updatedAt")] string? UpdatedAt = null
);

public static class OrderWithUserExtensions
{
    extension(OrderWithUser order)
    {
        public string GenerateWhisper(string itemName)
        {
            return order.Rank != null
                ? $"/w {order.User.IngameName} Hi! I want to buy: \"{itemName} (rank {order.Rank})\" for {order.Platinum} platinum. (warframe.market)"
                : $"/w {order.User.IngameName} Hi! I want to buy: \"{itemName}\" for {order.Platinum} platinum. (warframe.market)";
        }
    }
}