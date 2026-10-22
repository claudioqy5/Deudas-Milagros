using BodegaDebts.Api.Models;

namespace BodegaDebts.Api.DTOs;

public class CreateCreditSaleDto
{
    public string CustomerId { get; set; } = string.Empty;
    public List<CreateSaleItemDto> Items { get; set; } = new();
    public decimal InitialPayment { get; set; } = 0m;
    public string? Notes { get; set; }
}

public class CreateSaleItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; } = 0m;
}

public class CreatePaymentDto
{
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; } = 0m;
    public string PaymentMethod { get; set; } = "Efectivo";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class CustomerStatementDto
{
    public Customer Customer { get; set; } = new();
    public List<CreditSale> Sales { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public decimal TotalSalesAmount { get; set; }
    public decimal TotalPaymentsAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public string WhatsAppMessage { get; set; } = string.Empty;
    public string WhatsAppUrl { get; set; } = string.Empty;
}

public class DashboardSummaryDto
{
    public decimal TotalPendingDebt { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalSalesFiadas { get; set; }
    public int ActiveDebtorsCount { get; set; }
    public int TotalCustomersCount { get; set; }
    public int TotalProductsCount { get; set; }
    public int LowStockProductsCount { get; set; }
    public List<Customer> TopDebtors { get; set; } = new();
    public List<MonthlyMetricDto> MonthlyTrend { get; set; } = new();
    public List<RecentActivityDto> RecentActivity { get; set; } = new();
    public StoreSettings Store { get; set; } = new();
}

public class MonthlyMetricDto
{
    public string Month { get; set; } = string.Empty;
    public decimal TotalFiado { get; set; }
    public decimal TotalCobrado { get; set; }
}

public class RecentActivityDto
{
    public string Type { get; set; } = string.Empty; // "SALE" or "PAYMENT"
    public string Id { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
}
