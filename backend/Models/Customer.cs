using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BodegaDebts.Api.Models;

public class Customer
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("documentNumber")]
    public string? DocumentNumber { get; set; }

    [BsonElement("address")]
    public string? Address { get; set; }

    [BsonElement("creditLimit")]
    public decimal CreditLimit { get; set; } = 300m;

    [BsonElement("currentDebt")]
    public decimal CurrentDebt { get; set; } = 0m;

    [BsonElement("totalPurchased")]
    public decimal TotalPurchased { get; set; } = 0m;

    [BsonElement("totalPaid")]
    public decimal TotalPaid { get; set; } = 0m;

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("lastSaleDate")]
    public DateTime? LastSaleDate { get; set; }

    [BsonElement("lastPaymentDate")]
    public DateTime? LastPaymentDate { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
