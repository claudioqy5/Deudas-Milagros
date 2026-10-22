using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BodegaDebts.Api.Models;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = "Abarrotes";

    [BsonElement("salePrice")]
    public decimal SalePrice { get; set; } = 0m;

    [BsonElement("stock")]
    public decimal Stock { get; set; } = 0m;

    [BsonElement("isInfiniteStock")]
    public bool IsInfiniteStock { get; set; } = true;

    [BsonElement("unit")]
    public string Unit { get; set; } = "und";

    [BsonElement("imageUrl")]
    public string? ImageUrl { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
