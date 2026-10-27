using BodegaDebts.Api.Models;
using BodegaDebts.Api.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BodegaDebts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly MongoDbService _db;

    public ProductsController(MongoDbService db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category)
    {
        var filter = Builders<Product>.Filter.Eq(p => p.IsActive, true);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchFilter = Builders<Product>.Filter.Regex(p => p.Name, new MongoDB.Bson.BsonRegularExpression(search, "i"));
            filter = Builders<Product>.Filter.And(filter, searchFilter);
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "Todos")
        {
            filter = Builders<Product>.Filter.And(filter, Builders<Product>.Filter.Eq(p => p.Category, category));
        }

        var products = await _db.Products
            .Find(filter)
            .SortBy(p => p.Name)
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var categories = await _db.Products
            .Distinct<string>("category", Builders<Product>.Filter.Eq(p => p.IsActive, true))
            .ToListAsync();

        return Ok(categories.OrderBy(c => c).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(string id)
    {
        var product = await _db.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (product == null) return NotFound(new { message = "Producto no encontrado" });
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return BadRequest(new { message = "El nombre del producto es obligatorio" });
        }

        product.Id = null;
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        product.IsActive = true;

        await _db.Products.InsertOneAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> Update(string id, [FromBody] Product update)
    {
        var existing = await _db.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound(new { message = "Producto no encontrado" });

        existing.Name = update.Name;
        existing.Category = update.Category;
        existing.SalePrice = update.SalePrice;
        existing.Stock = update.Stock;
        existing.IsInfiniteStock = update.IsInfiniteStock;
        existing.Unit = update.Unit;
        existing.ImageUrl = update.ImageUrl;
        existing.Packs = update.Packs ?? new();
        existing.IsCombo = update.IsCombo;
        existing.ComboItems = update.ComboItems ?? new();
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.Products.ReplaceOneAsync(p => p.Id == id, existing);
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var existing = await _db.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound();

        existing.IsActive = false;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.Products.ReplaceOneAsync(p => p.Id == id, existing);

        return Ok(new { message = "Producto archivado" });
    }
}
