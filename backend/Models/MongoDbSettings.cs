namespace BodegaDebts.Api.Models;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "BodegaDebtsDb";
}

public class StoreSettings
{
    public string StoreName { get; set; } = "Bodega & Minimarket Don Pepe";
    public string OwnerName { get; set; } = "Don Pepe";
    public string Phone { get; set; } = "987654321";
    public string YapeNumber { get; set; } = "987 654 321";
    public string PlinNumber { get; set; } = "987 654 321";
    public string CurrencySymbol { get; set; } = "S/.";
}
