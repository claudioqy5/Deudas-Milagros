using BodegaDebts.Api.DTOs;
using BodegaDebts.Api.Models;
using BodegaDebts.Api.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BodegaDebts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly MongoDbService _db;

    public CustomersController(MongoDbService db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<Customer>>> GetAll([FromQuery] string? search, [FromQuery] bool onlyDebtors = false)
    {
        var filter = Builders<Customer>.Filter.Eq(c => c.IsActive, true);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchFilter = Builders<Customer>.Filter.Or(
                Builders<Customer>.Filter.Regex(c => c.Name, new MongoDB.Bson.BsonRegularExpression(search, "i")),
                Builders<Customer>.Filter.Regex(c => c.Phone, new MongoDB.Bson.BsonRegularExpression(search, "i")),
                Builders<Customer>.Filter.Regex(c => c.DocumentNumber, new MongoDB.Bson.BsonRegularExpression(search, "i"))
            );
            filter = Builders<Customer>.Filter.And(filter, searchFilter);
        }

        if (onlyDebtors)
        {
            filter = Builders<Customer>.Filter.And(filter, Builders<Customer>.Filter.Gt(c => c.CurrentDebt, 0));
        }

        var customers = await _db.Customers
            .Find(filter)
            .SortByDescending(c => c.CurrentDebt)
            .ToListAsync();

        return Ok(customers);
    }

    [HttpGet("top-debtors")]
    public async Task<ActionResult<List<Customer>>> GetTopDebtors([FromQuery] int limit = 10)
    {
        var debtors = await _db.Customers
            .Find(c => c.IsActive && c.CurrentDebt > 0)
            .SortByDescending(c => c.CurrentDebt)
            .Limit(limit)
            .ToListAsync();

        return Ok(debtors);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetById(string id)
    {
        var customer = await _db.Customers.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (customer == null) return NotFound(new { message = "Cliente no encontrado" });
        return Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> Create([FromBody] Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Name))
        {
            return BadRequest(new { message = "El nombre del cliente es obligatorio" });
        }

        customer.Id = null;
        customer.CreatedAt = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;
        customer.CurrentDebt = 0;
        customer.TotalPurchased = 0;
        customer.TotalPaid = 0;
        customer.IsActive = true;

        await _db.Customers.InsertOneAsync(customer);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Customer>> Update(string id, [FromBody] Customer updateDto)
    {
        var existing = await _db.Customers.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound(new { message = "Cliente no encontrado" });

        existing.Name = updateDto.Name;
        existing.Phone = updateDto.Phone;
        existing.DocumentNumber = updateDto.DocumentNumber;
        existing.Address = updateDto.Address;
        existing.CreditLimit = updateDto.CreditLimit;
        existing.Notes = updateDto.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.Customers.ReplaceOneAsync(c => c.Id == id, existing);
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var existing = await _db.Customers.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound();

        if (existing.CurrentDebt > 0)
        {
            return BadRequest(new { message = "No se puede eliminar un cliente con deuda pendiente activa." });
        }

        existing.IsActive = false;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.Customers.ReplaceOneAsync(c => c.Id == id, existing);

        return Ok(new { message = "Cliente archivado correctamente" });
    }

    [HttpGet("{id}/statement")]
    public async Task<ActionResult<CustomerStatementDto>> GetCustomerStatement(string id)
    {
        var customer = await _db.Customers.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (customer == null) return NotFound(new { message = "Cliente no encontrado" });

        var sales = await _db.CreditSales
            .Find(s => s.CustomerId == id)
            .SortByDescending(s => s.Date)
            .ToListAsync();

        var payments = await _db.Payments
            .Find(p => p.CustomerId == id)
            .SortByDescending(p => p.Date)
            .ToListAsync();

        // Calculate pending sales (sales that are not fully paid)
        var pendingSales = sales.Where(s => s.Status != "Paid").ToList();

        // Generate WhatsApp notification text
        var message = _db.BuildWhatsAppMessage(customer, pendingSales, payments, customer.CurrentDebt);
        var waUrl = _db.BuildWhatsAppUrl(customer.Phone, message);

        var statement = new CustomerStatementDto
        {
            Customer = customer,
            Sales = sales,
            Payments = payments,
            TotalSalesAmount = customer.TotalPurchased,
            TotalPaymentsAmount = customer.TotalPaid,
            BalanceDue = customer.CurrentDebt,
            WhatsAppMessage = message,
            WhatsAppUrl = waUrl
        };

        return Ok(statement);
    }
}
