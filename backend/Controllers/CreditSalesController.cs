using BodegaDebts.Api.DTOs;
using BodegaDebts.Api.Models;
using BodegaDebts.Api.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BodegaDebts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CreditSalesController : ControllerBase
{
    private readonly MongoDbService _db;

    public CreditSalesController(MongoDbService db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<CreditSale>>> GetAll(
        [FromQuery] string? customerId,
        [FromQuery] string? status,
        [FromQuery] int limit = 50)
    {
        var filter = Builders<CreditSale>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            filter = Builders<CreditSale>.Filter.And(filter, Builders<CreditSale>.Filter.Eq(s => s.CustomerId, customerId));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            filter = Builders<CreditSale>.Filter.And(filter, Builders<CreditSale>.Filter.Eq(s => s.Status, status));
        }

        var sales = await _db.CreditSales
            .Find(filter)
            .SortByDescending(s => s.Date)
            .Limit(limit)
            .ToListAsync();

        return Ok(sales);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CreditSale>> GetById(string id)
    {
        var sale = await _db.CreditSales.Find(s => s.Id == id).FirstOrDefaultAsync();
        if (sale == null) return NotFound(new { message = "Venta no encontrada" });
        return Ok(sale);
    }

    [HttpPost]
    public async Task<ActionResult<CreditSale>> CreateCreditSale([FromBody] CreateCreditSaleDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            return BadRequest(new { message = "Debe agregar al menos un producto a la venta fiada." });
        }

        var customer = await _db.Customers.Find(c => c.Id == dto.CustomerId).FirstOrDefaultAsync();
        if (customer == null)
        {
            return BadRequest(new { message = "Cliente no encontrado o no especificado." });
        }

        var saleItems = new List<SaleItem>();
        decimal totalAmount = 0m;

        foreach (var reqItem in dto.Items)
        {
            if (reqItem.Quantity <= 0) continue;

            var product = await _db.Products.Find(p => p.Id == reqItem.ProductId).FirstOrDefaultAsync();
            if (product == null)
            {
                return BadRequest(new { message = $"Producto con ID {reqItem.ProductId} no encontrado." });
            }

            var unitPrice = reqItem.UnitPrice > 0 ? reqItem.UnitPrice : product.SalePrice;
            var subtotal = Math.Round(reqItem.Quantity * unitPrice, 2);

            var itemName = !string.IsNullOrWhiteSpace(reqItem.ProductName) 
                ? reqItem.ProductName 
                : product.Name;

            saleItems.Add(new SaleItem
            {
                ProductId = product.Id!,
                ProductName = itemName,
                Quantity = reqItem.Quantity,
                UnitPrice = unitPrice,
                Subtotal = subtotal
            });

            totalAmount += subtotal;

            // Deduct stock
            if (product.IsCombo && product.ComboItems != null && product.ComboItems.Count > 0)
            {
                foreach (var cItem in product.ComboItems)
                {
                    var compProduct = await _db.Products.Find(p => p.Id == cItem.ProductId).FirstOrDefaultAsync();
                    if (compProduct != null && !compProduct.IsInfiniteStock)
                    {
                        var deduction = reqItem.Quantity * cItem.Quantity;
                        compProduct.Stock -= deduction;
                        compProduct.UpdatedAt = DateTime.UtcNow;
                        await _db.Products.ReplaceOneAsync(p => p.Id == compProduct.Id, compProduct);
                    }
                }
            }
            else if (!product.IsInfiniteStock)
            {
                var deduction = reqItem.UnitsDeducted > 0 ? reqItem.UnitsDeducted : reqItem.Quantity;
                product.Stock -= deduction;
                product.UpdatedAt = DateTime.UtcNow;
                await _db.Products.ReplaceOneAsync(p => p.Id == product.Id, product);
            }
        }

        var countToday = await _db.CreditSales.CountDocumentsAsync(_ => true);
        var saleCode = $"FIA-{DateTime.UtcNow:yyyyMMdd}-{(countToday + 1):D3}";

        var initialPayment = Math.Min(dto.InitialPayment, totalAmount);
        var remainingDebt = totalAmount - initialPayment;

        string status = "Pending";
        if (remainingDebt <= 0)
        {
            status = "Paid";
        }
        else if (initialPayment > 0)
        {
            status = "PartiallyPaid";
        }

        var creditSale = new CreditSale
        {
            SaleCode = saleCode,
            CustomerId = customer.Id!,
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone,
            Items = saleItems,
            TotalAmount = totalAmount,
            InitialPayment = initialPayment,
            RemainingDebt = remainingDebt,
            Status = status,
            Date = DateTime.UtcNow,
            Notes = dto.Notes
        };

        await _db.CreditSales.InsertOneAsync(creditSale);

        customer.CurrentDebt += remainingDebt;
        customer.TotalPurchased += totalAmount;
        customer.TotalPaid += initialPayment;
        customer.LastSaleDate = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;

        await _db.Customers.ReplaceOneAsync(c => c.Id == customer.Id, customer);

        if (initialPayment > 0)
        {
            var payment = new Payment
            {
                CustomerId = customer.Id!,
                CustomerName = customer.Name,
                Amount = initialPayment,
                PaymentMethod = "Efectivo",
                Reference = $"Adelanto {saleCode}",
                Date = DateTime.UtcNow,
                Notes = $"Pago inicial al registrar fiado {saleCode}"
            };
            await _db.Payments.InsertOneAsync(payment);
        }

        return CreatedAtAction(nameof(GetById), new { id = creditSale.Id }, creditSale);
    }
}
