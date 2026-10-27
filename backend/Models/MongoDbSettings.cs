namespace BodegaDebts.Api.Models;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "BodegaDebtsDb";
}

public class StoreSettings
{
    public string StoreName { get; set; } = "Bodega & Minimarket Milagros";
    public string OwnerName { get; set; } = "Milagros";
    public string Phone { get; set; } = "983519562";
    public string YapeNumber { get; set; } = "983 519 562";
    public string PlinNumber { get; set; } = "983 519 562";
    public string CurrencySymbol { get; set; } = "S/.";
}
