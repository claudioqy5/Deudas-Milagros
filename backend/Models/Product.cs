using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BodegaDebts.Api.Models;

[BsonIgnoreExtraElements]
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

    [BsonElement("packs")]
    public List<ProductPack> Packs { get; set; } = new();

    [BsonElement("isCombo")]
    public bool IsCombo { get; set; } = false;

    [BsonElement("comboItems")]
    public List<ComboItem> ComboItems { get; set; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
public class ComboItem
{
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public decimal Quantity { get; set; } = 1m;

    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; } = 0m;
}

[BsonIgnoreExtraElements]
public class ProductPack
{
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty; // e.g. "Sixpack agua", "Caja x24", "Pack x3"

    [BsonElement("unitsCount")]
    public decimal UnitsCount { get; set; } = 1m; // how many base units are contained

    [BsonElement("price")]
    public decimal Price { get; set; } = 0m; // specific price for the pack

    [BsonElement("imageUrl")]
    public string? ImageUrl { get; set; } // custom photo for the pack
}
