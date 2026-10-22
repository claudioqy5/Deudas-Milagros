using BodegaDebts.Api.DTOs;
using BodegaDebts.Api.Models;
using BodegaDebts.Api.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BodegaDebts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly MongoDbService _db;

    public DashboardController(MongoDbService db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var customers = await _db.Customers.Find(c => c.IsActive).ToListAsync();
        var sales = await _db.CreditSales.Find(_ => true).ToListAsync();
        var payments = await _db.Payments.Find(_ => true).ToListAsync();
        var products = await _db.Products.Find(p => p.IsActive).ToListAsync();

        var totalPendingDebt = customers.Sum(c => c.CurrentDebt);
        var totalCollected = payments.Sum(p => p.Amount) + sales.Sum(s => s.InitialPayment);
        var totalSalesFiadas = sales.Sum(s => s.TotalAmount);

        var activeDebtors = customers.Where(c => c.CurrentDebt > 0).OrderByDescending(c => c.CurrentDebt).ToList();
        var topDebtors = activeDebtors.Take(5).ToList();

        var lowStockCount = products.Count(p => !p.IsInfiniteStock && p.Stock <= 5);

        var now = DateTime.UtcNow;
        var monthlyTrends = new List<MonthlyMetricDto>();

        for (int i = 5; i >= 0; i--)
        {
            var targetMonthDate = now.AddMonths(-i);
            var year = targetMonthDate.Year;
            var month = targetMonthDate.Month;
            var monthName = targetMonthDate.ToString("MMM yyyy");

            var monthSales = sales
                .Where(s => s.Date.Year == year && s.Date.Month == month)
                .Sum(s => s.TotalAmount);

            var monthPayments = payments
                .Where(p => p.Date.Year == year && p.Date.Month == month)
                .Sum(p => p.Amount) +
                sales
                .Where(s => s.Date.Year == year && s.Date.Month == month)
                .Sum(s => s.InitialPayment);

            monthlyTrends.Add(new MonthlyMetricDto
            {
                Month = monthName,
                TotalFiado = monthSales,
                TotalCobrado = monthPayments
            });
        }

        var recentSales = sales.OrderByDescending(s => s.Date).Take(10).Select(s => new RecentActivityDto
        {
            Type = "SALE",
            Id = s.Id!,
            CustomerName = s.CustomerName,
            Amount = s.TotalAmount,
            Date = s.Date,
            Description = $"Fiado de {s.Items.Count} producto(s) ({s.SaleCode})"
        });

        var recentPayments = payments.OrderByDescending(p => p.Date).Take(10).Select(p => new RecentActivityDto
        {
            Type = "PAYMENT",
            Id = p.Id!,
            CustomerName = p.CustomerName,
            Amount = p.Amount,
            Date = p.Date,
            Description = $"Abono recibido vía {p.PaymentMethod}"
        });

        var combinedActivity = recentSales.Concat(recentPayments)
            .OrderByDescending(a => a.Date)
            .Take(8)
            .ToList();

        var summary = new DashboardSummaryDto
        {
            TotalPendingDebt = totalPendingDebt,
            TotalCollected = totalCollected,
            TotalSalesFiadas = totalSalesFiadas,
            ActiveDebtorsCount = activeDebtors.Count,
            TotalCustomersCount = customers.Count,
            TotalProductsCount = products.Count,
            LowStockProductsCount = lowStockCount,
            TopDebtors = topDebtors,
            MonthlyTrend = monthlyTrends,
            RecentActivity = combinedActivity,
            Store = _db.StoreConfig
        };

        return Ok(summary);
    }
}
