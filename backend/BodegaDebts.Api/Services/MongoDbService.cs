using BodegaDebts.Api.Models;
using MongoDB.Driver;

namespace BodegaDebts.Api.Services;

public class MongoDbService
{
    private readonly IMongoDatabase _database;
    public StoreSettings StoreConfig { get; }

    public MongoDbService(IConfiguration configuration)
    {
        var connStr = configuration.GetValue<string>("MongoDbSettings:ConnectionString") ?? "mongodb://localhost:27017";
        var dbName = configuration.GetValue<string>("MongoDbSettings:DatabaseName") ?? "BodegaDebtsDb";

        var client = new MongoClient(connStr);
        _database = client.GetDatabase(dbName);

        StoreConfig = new StoreSettings();
        configuration.GetSection("StoreSettings").Bind(StoreConfig);
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Customer> Customers => _database.GetCollection<Customer>("customers");
    public IMongoCollection<Product> Products => _database.GetCollection<Product>("products");
    public IMongoCollection<CreditSale> CreditSales => _database.GetCollection<CreditSale>("credit_sales");
    public IMongoCollection<Payment> Payments => _database.GetCollection<Payment>("payments");

    public string BuildWhatsAppMessage(Customer customer, List<CreditSale> pendingSales, List<Payment> recentPayments, decimal currentDebt)
    {
        var store = StoreConfig;
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"*{store.StoreName.ToUpper()}*");
        sb.AppendLine("--------------------------------------------------");
        sb.AppendLine($"Estimado(a) *{customer.Name.Trim()}*, le compartimos el detalle actualizado de sus compras pendientes:");
        sb.AppendLine();

        if (pendingSales.Count == 0)
        {
            sb.AppendLine("*No registra deudas pendientes en este momento.*");
            sb.AppendLine("Agradecemos mantener sus cuentas al dia. Lo esperamos pronto.");
        }
        else
        {
            foreach (var sale in pendingSales.OrderBy(s => s.Date))
            {
                var dateStr = sale.Date.ToLocalTime().ToString("dd/MM/yyyy hh:mm tt");
                sb.AppendLine($"*Fecha:* {dateStr} ({sale.SaleCode})");

                foreach (var item in sale.Items)
                {
                    sb.AppendLine($"  - {item.Quantity:0.##} x {item.ProductName} ({store.CurrencySymbol} {item.UnitPrice:N2}) = *{store.CurrencySymbol} {item.Subtotal:N2}*");
                }

                if (sale.InitialPayment > 0)
                {
                    sb.AppendLine($"    (Adelanto entregado: {store.CurrencySymbol} {sale.InitialPayment:N2})");
                }

                sb.AppendLine($"  Subtotal pendiente: {store.CurrencySymbol} {sale.RemainingDebt:N2}");
                sb.AppendLine();
            }

            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine("*RESUMEN DE CUENTA:*");
            sb.AppendLine($"*Total Pendiente por Cobrar:* *{store.CurrencySymbol} {currentDebt:N2}*");
            
            if (recentPayments.Count > 0)
            {
                var ultimoAbono = recentPayments.OrderByDescending(p => p.Date).First();
                sb.AppendLine($"Ultimo abono registrado: {store.CurrencySymbol} {ultimoAbono.Amount:N2} ({ultimoAbono.Date.ToLocalTime():dd/MM/yyyy})");
            }

            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine();
            sb.AppendLine("*METODOS DE PAGO DISPONIBLES:*");
            if (!string.IsNullOrWhiteSpace(store.YapeNumber))
            {
                sb.AppendLine($"Yape / Plin: {store.YapeNumber} ({store.OwnerName})");
            }
            sb.AppendLine("O puede acercarse directamente a la bodega.");
            sb.AppendLine();
            sb.AppendLine("Agradecemos su preferencia y puntualidad.");
        }

        return sb.ToString().Trim();
    }

    public string BuildWhatsAppUrl(string rawPhone, string message)
    {
        if (string.IsNullOrWhiteSpace(rawPhone)) return string.Empty;

        var clean = new string(rawPhone.Where(char.IsDigit).ToArray());

        if (clean.Length == 9 && clean.StartsWith("9"))
        {
            clean = "51" + clean;
        }

        var encodedMsg = Uri.EscapeDataString(message);
        return $"https://wa.me/{clean}?text={encodedMsg}";
    }
}
