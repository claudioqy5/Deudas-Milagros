using BodegaDebts.Api.DTOs;
using BodegaDebts.Api.Models;
using BodegaDebts.Api.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BodegaDebts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly MongoDbService _db;

    public PaymentsController(MongoDbService db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<Payment>>> GetAll(
        [FromQuery] string? customerId,
        [FromQuery] int limit = 50)
    {
        var filter = Builders<Payment>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            filter = Builders<Payment>.Filter.And(filter, Builders<Payment>.Filter.Eq(p => p.CustomerId, customerId));
        }

        var payments = await _db.Payments
            .Find(filter)
            .SortByDescending(p => p.Date)
            .Limit(limit)
            .ToListAsync();

        return Ok(payments);
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "El monto del abono debe ser mayor a 0." });
        }

        var customer = await _db.Customers.Find(c => c.Id == dto.CustomerId).FirstOrDefaultAsync();
        if (customer == null)
        {
            return BadRequest(new { message = "Cliente no encontrado." });
        }

        var payment = new Payment
        {
            CustomerId = customer.Id!,
            CustomerName = customer.Name,
            Amount = dto.Amount,
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Efectivo" : dto.PaymentMethod,
            Reference = dto.Reference,
            Notes = dto.Notes,
            Date = DateTime.UtcNow
        };

        await _db.Payments.InsertOneAsync(payment);

        // Update customer
        customer.CurrentDebt = Math.Max(0, customer.CurrentDebt - dto.Amount);
        customer.TotalPaid += dto.Amount;
        customer.LastPaymentDate = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;
        await _db.Customers.ReplaceOneAsync(c => c.Id == customer.Id, customer);

        // Distribute payment across pending sales (oldest to newest)
        var pendingSales = await _db.CreditSales
            .Find(s => s.CustomerId == customer.Id && s.Status != "Paid")
            .SortBy(s => s.Date)
            .ToListAsync();

        var remainingPayment = dto.Amount;
        foreach (var sale in pendingSales)
        {
            if (remainingPayment <= 0) break;

            if (remainingPayment >= sale.RemainingDebt)
            {
                remainingPayment -= sale.RemainingDebt;
                sale.RemainingDebt = 0;
                sale.Status = "Paid";
            }
            else
            {
                sale.RemainingDebt -= remainingPayment;
                remainingPayment = 0;
                sale.Status = "PartiallyPaid";
            }

            await _db.CreditSales.ReplaceOneAsync(s => s.Id == sale.Id, sale);
        }

        return Ok(new
        {
            payment,
            updatedDebt = customer.CurrentDebt,
            customerName = customer.Name
        });
    }
}
