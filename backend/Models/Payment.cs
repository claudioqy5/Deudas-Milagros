using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BodegaDebts.Api.Models;

public class Payment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [BsonElement("amount")]
    public decimal Amount { get; set; } = 0m;

    [BsonElement("paymentMethod")]
    public string PaymentMethod { get; set; } = "Efectivo"; // Efectivo, Yape, Plin, Transferencia

    [BsonElement("reference")]
    public string? Reference { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;
}
