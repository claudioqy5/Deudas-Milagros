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
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] string period = "all",
        [FromQuery] string? date = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var customers = await _db.Customers.Find(c => c.IsActive).ToListAsync();
        var allSales = await _db.CreditSales.Find(_ => true).ToListAsync();
        var allPayments = await _db.Payments.Find(_ => true).ToListAsync();
        var products = await _db.Products.Find(p => p.IsActive).ToListAsync();

        var globalPendingDebt = customers.Sum(c => c.CurrentDebt);
        var lowStockCount = products.Count(p => !p.IsInfiniteStock && p.Stock <= 5);

        period = (period ?? "all").ToLowerInvariant();
        var esCulture = new System.Globalization.CultureInfo("es-PE");

        DateTime refDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
        {
            refDate = parsedDate;
        }

        DateTime startUtc;
        DateTime endUtc;
        string periodLabel;
        var trendMetrics = new List<MonthlyMetricDto>();

        if (startDate.HasValue && endDate.HasValue)
        {
            startUtc = startDate.Value.ToUniversalTime();
            endUtc = endDate.Value.ToUniversalTime();
            periodLabel = $"{startUtc:dd/MM/yyyy} - {endUtc:dd/MM/yyyy}";
        }
        else
        {
            switch (period)
            {
                case "day":
                    startUtc = new DateTime(refDate.Year, refDate.Month, refDate.Day, 0, 0, 0, DateTimeKind.Utc);
                    endUtc = startUtc.AddDays(1).AddTicks(-1);
                    periodLabel = refDate.ToString("dd 'de' MMMM, yyyy", esCulture);
                    break;
                case "week":
                    var diff = (7 + (refDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                    var monday = refDate.AddDays(-diff).Date;
                    startUtc = new DateTime(monday.Year, monday.Month, monday.Day, 0, 0, 0, DateTimeKind.Utc);
                    endUtc = startUtc.AddDays(7).AddTicks(-1);
                    var endWeek = startUtc.AddDays(6);
                    periodLabel = $"Semana del {startUtc:dd 'de' MMM} al {endWeek:dd 'de' MMM, yyyy}";
                    break;
                case "month":
                    startUtc = new DateTime(refDate.Year, refDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    endUtc = startUtc.AddMonths(1).AddTicks(-1);
                    periodLabel = refDate.ToString("MMMM yyyy", esCulture);
                    break;
                case "year":
                    startUtc = new DateTime(refDate.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    endUtc = new DateTime(refDate.Year, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
                    periodLabel = $"Año {refDate.Year}";
                    break;
                default:
                    period = "all";
                    startUtc = DateTime.MinValue;
                    endUtc = DateTime.MaxValue;
                    periodLabel = "Histórico Completo";
                    break;
            }
        }

        // Generate trend intervals based on period
        switch (period)
        {
            case "day":
                periodLabel = refDate.ToString("dd 'de' MMMM, yyyy", esCulture);
                var daySlots = new (string Label, int StartHour, int EndHour)[]
                {
                    ("00:00 - 06:00", 0, 6),
                    ("06:00 - 09:00", 6, 9),
                    ("09:00 - 12:00", 9, 12),
                    ("12:00 - 15:00", 12, 15),
                    ("15:00 - 18:00", 15, 18),
                    ("18:00 - 21:00", 18, 21),
                    ("21:00 - 24:00", 21, 24)
                };

                foreach (var slot in daySlots)
                {
                    var sTime = startUtc.AddHours(slot.StartHour);
                    var eTime = startUtc.AddHours(slot.EndHour);

                    var slotSales = allSales.Where(s => s.Date >= sTime && s.Date < eTime).ToList();
                    var slotPayments = allPayments.Where(p => p.Date >= sTime && p.Date < eTime).ToList();

                    trendMetrics.Add(new MonthlyMetricDto
                    {
                        Month = slot.Label,
                        TotalFiado = slotSales.Sum(s => s.TotalAmount),
                        TotalCobrado = slotPayments.Sum(p => p.Amount) + slotSales.Sum(s => s.InitialPayment)
                    });
                }
                break;

            case "week":
                var endDay = startUtc.AddDays(6);
                periodLabel = $"Semana del {startUtc:dd 'de' MMM} al {endDay:dd 'de' MMM, yyyy}";
                string[] dayNames = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

                for (int i = 0; i < 7; i++)
                {
                    var dayStart = startUtc.AddDays(i);
                    var dayEnd = dayStart.AddDays(1);

                    var dSales = allSales.Where(s => s.Date >= dayStart && s.Date < dayEnd).ToList();
                    var dPayments = allPayments.Where(p => p.Date >= dayStart && p.Date < dayEnd).ToList();

                    trendMetrics.Add(new MonthlyMetricDto
                    {
                        Month = $"{dayNames[i]} {dayStart:dd/MM}",
                        TotalFiado = dSales.Sum(s => s.TotalAmount),
                        TotalCobrado = dPayments.Sum(p => p.Amount) + dSales.Sum(s => s.InitialPayment)
                    });
                }
                break;

            case "month":
                periodLabel = refDate.ToString("MMMM yyyy", esCulture);
                var daysInMonth = DateTime.DaysInMonth(refDate.Year, refDate.Month);
                var monthSlots = new List<(string Label, int StartDay, int EndDay)>
                {
                    ("Sem 1 (1-7)", 1, 7),
                    ("Sem 2 (8-14)", 8, 14),
                    ("Sem 3 (15-21)", 15, 21),
                    ("Sem 4 (22-28)", 22, 28)
                };
                if (daysInMonth > 28)
                {
                    monthSlots.Add(($"Sem 5 (29-{daysInMonth})", 29, daysInMonth));
                }

                foreach (var slot in monthSlots)
                {
                    var slotStart = startUtc.AddDays(slot.StartDay - 1);
                    var slotEnd = startUtc.AddDays(slot.EndDay);

                    var mSales = allSales.Where(s => s.Date >= slotStart && s.Date < slotEnd).ToList();
                    var mPayments = allPayments.Where(p => p.Date >= slotStart && p.Date < slotEnd).ToList();

                    trendMetrics.Add(new MonthlyMetricDto
                    {
                        Month = slot.Label,
                        TotalFiado = mSales.Sum(s => s.TotalAmount),
                        TotalCobrado = mPayments.Sum(p => p.Amount) + mSales.Sum(s => s.InitialPayment)
                    });
                }
                break;

            case "year":
                periodLabel = $"Año {refDate.Year}";
                string[] monthNames = { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Set", "Oct", "Nov", "Dic" };
                for (int m = 1; m <= 12; m++)
                {
                    var mStart = new DateTime(refDate.Year, m, 1, 0, 0, 0, DateTimeKind.Utc);
                    var mEnd = mStart.AddMonths(1);

                    var ySales = allSales.Where(s => s.Date >= mStart && s.Date < mEnd).ToList();
                    var yPayments = allPayments.Where(p => p.Date >= mStart && p.Date < mEnd).ToList();

                    trendMetrics.Add(new MonthlyMetricDto
                    {
                        Month = monthNames[m - 1],
                        TotalFiado = ySales.Sum(s => s.TotalAmount),
                        TotalCobrado = yPayments.Sum(p => p.Amount) + ySales.Sum(s => s.InitialPayment)
                    });
                }
                break;

            default:
                periodLabel = "Histórico Completo";
                var now = DateTime.UtcNow;
                for (int i = 5; i >= 0; i--)
                {
                    var targetMonthDate = now.AddMonths(-i);
                    var year = targetMonthDate.Year;
                    var month = targetMonthDate.Month;
                    var monthName = targetMonthDate.ToString("MMM yyyy", esCulture);

                    var monthSales = allSales
                        .Where(s => s.Date.Year == year && s.Date.Month == month)
                        .Sum(s => s.TotalAmount);

                    var monthPayments = allPayments
                        .Where(p => p.Date.Year == year && p.Date.Month == month)
                        .Sum(p => p.Amount) +
                        allSales
                        .Where(s => s.Date.Year == year && s.Date.Month == month)
                        .Sum(s => s.InitialPayment);

                    trendMetrics.Add(new MonthlyMetricDto
                    {
                        Month = monthName,
                        TotalFiado = monthSales,
                        TotalCobrado = monthPayments
                    });
                }
                break;
        }

        // Filter transactions according to selected range
        List<CreditSale> filteredSales;
        List<Payment> filteredPayments;

        if (period == "all")
        {
            filteredSales = allSales;
            filteredPayments = allPayments;
        }
        else
        {
            filteredSales = allSales.Where(s => s.Date >= startUtc && s.Date <= endUtc).ToList();
            filteredPayments = allPayments.Where(p => p.Date >= startUtc && p.Date <= endUtc).ToList();
        }

        var totalSalesFiadas = filteredSales.Sum(s => s.TotalAmount);
        var totalCollected = filteredPayments.Sum(p => p.Amount) + filteredSales.Sum(s => s.InitialPayment);
        var totalPendingDebt = period == "all"
            ? globalPendingDebt
            : filteredSales.Sum(s => s.RemainingDebt);

        var activeDebtorsCount = period == "all"
            ? customers.Count(c => c.CurrentDebt > 0)
            : filteredSales.Where(s => s.RemainingDebt > 0).Select(s => s.CustomerId).Distinct().Count();

        // Top Debtors
        List<Customer> topDebtors;
        if (period == "all")
        {
            topDebtors = customers.Where(c => c.CurrentDebt > 0)
                .OrderByDescending(c => c.CurrentDebt)
                .Take(5)
                .ToList();
        }
        else
        {
            var groupedByCustomer = filteredSales.GroupBy(s => s.CustomerId).ToList();
            var periodDebtorList = new List<Customer>();

            foreach (var g in groupedByCustomer)
            {
                var cust = customers.FirstOrDefault(c => c.Id == g.Key);
                var remDebt = g.Sum(s => s.RemainingDebt);
                var totalFiadoInPeriod = g.Sum(s => s.TotalAmount);

                if (cust != null)
                {
                    periodDebtorList.Add(new Customer
                    {
                        Id = cust.Id,
                        Name = cust.Name,
                        Phone = cust.Phone,
                        CreditLimit = cust.CreditLimit,
                        CurrentDebt = remDebt > 0 ? remDebt : cust.CurrentDebt,
                        TotalPurchased = totalFiadoInPeriod,
                        TotalPaid = cust.TotalPaid,
                        DocumentNumber = cust.DocumentNumber,
                        Address = cust.Address,
                        Notes = cust.Notes,
                        IsActive = cust.IsActive,
                        LastSaleDate = cust.LastSaleDate,
                        LastPaymentDate = cust.LastPaymentDate,
                        CreatedAt = cust.CreatedAt
                    });
                }
            }

            topDebtors = periodDebtorList
                .OrderByDescending(c => c.CurrentDebt)
                .ThenByDescending(c => c.TotalPurchased)
                .Take(5)
                .ToList();
        }

        var recentSales = filteredSales.OrderByDescending(s => s.Date).Take(15).Select(s => new RecentActivityDto
        {
            Type = "SALE",
            Id = s.Id!,
            CustomerName = s.CustomerName,
            Amount = s.TotalAmount,
            Date = s.Date,
            Description = $"Fiado de {s.Items.Count} producto(s) ({s.SaleCode})"
        });

        var recentPayments = filteredPayments.OrderByDescending(p => p.Date).Take(15).Select(p => new RecentActivityDto
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
            .Take(10)
            .ToList();

        var summary = new DashboardSummaryDto
        {
            TotalPendingDebt = totalPendingDebt,
            TotalCollected = totalCollected,
            TotalSalesFiadas = totalSalesFiadas,
            ActiveDebtorsCount = activeDebtorsCount,
            TotalCustomersCount = customers.Count,
            TotalProductsCount = products.Count,
            LowStockProductsCount = lowStockCount,
            TopDebtors = topDebtors,
            MonthlyTrend = trendMetrics,
            RecentActivity = combinedActivity,
            Store = _db.StoreConfig,
            Period = period,
            PeriodLabel = periodLabel,
            GlobalPendingDebt = globalPendingDebt,
            PeriodSalesCount = filteredSales.Count,
            PeriodPaymentsCount = filteredPayments.Count
        };

        return Ok(summary);
    }
}
