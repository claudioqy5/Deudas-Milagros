using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BodegaDebts.Api.Models;

public class CreditSale
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("saleCode")]
    public string SaleCode { get; set; } = string.Empty;

    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [BsonElement("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [BsonElement("items")]
    public List<SaleItem> Items { get; set; } = new();

    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; } = 0m;

    [BsonElement("initialPayment")]
    public decimal InitialPayment { get; set; } = 0m;

    [BsonElement("remainingDebt")]
    public decimal RemainingDebt { get; set; } = 0m;

    [BsonElement("status")]
    public string Status { get; set; } = "Pending"; // Pending, PartiallyPaid, Paid

    [BsonElement("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [BsonElement("notes")]
    public string? Notes { get; set; }
}

public class SaleItem
{
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public decimal Quantity { get; set; } = 1m;

    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; } = 0m;

    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; } = 0m;
}
