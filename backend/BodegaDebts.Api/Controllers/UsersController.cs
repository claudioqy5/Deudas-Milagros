using BodegaDebts.Api.DTOs;
using BodegaDebts.Api.Models;
using BodegaDebts.Api.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BodegaDebts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly MongoDbService _db;

    public UsersController(MongoDbService db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _db.Users
            .Find(u => u.IsActive)
            .SortByDescending(u => u.CreatedAt)
            .ToListAsync();

        var dtos = users.Select(u => new UserDto
        {
            Id = u.Id!,
            Username = u.Username,
            FullName = u.FullName,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLogin = u.LastLogin
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(string id)
    {
        var u = await _db.Users.Find(user => user.Id == id).FirstOrDefaultAsync();
        if (u == null) return NotFound(new { message = "Usuario no encontrado" });

        return Ok(new UserDto
        {
            Id = u.Id!,
            Username = u.Username,
            FullName = u.FullName,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLogin = u.LastLogin
        });
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Usuario y contraseña son requeridos" });
        }

        var normalized = dto.Username.Trim().ToLower();
        var existing = await _db.Users.Find(u => u.Username.ToLower() == normalized).FirstOrDefaultAsync();
        if (existing != null)
        {
            return BadRequest(new { message = "El nombre de usuario ya se encuentra registrado" });
        }

        var user = new User
        {
            Username = dto.Username.Trim(),
            PasswordHash = AuthController.HashPassword(dto.Password),
            FullName = string.IsNullOrWhiteSpace(dto.FullName) ? dto.Username.Trim() : dto.FullName.Trim(),
            Role = dto.Role == "Admin" ? "Admin" : "Worker",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Users.InsertOneAsync(user);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new UserDto
        {
            Id = user.Id!,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(string id, [FromBody] UpdateUserDto dto)
    {
        var user = await _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null) return NotFound(new { message = "Usuario no encontrado" });

        if (!string.IsNullOrWhiteSpace(dto.FullName))
        {
            user.FullName = dto.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            user.Role = dto.Role == "Admin" ? "Admin" : "Worker";
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = AuthController.HashPassword(dto.Password);
        }

        if (dto.IsActive.HasValue)
        {
            user.IsActive = dto.IsActive.Value;
        }

        await _db.Users.ReplaceOneAsync(u => u.Id == id, user);

        return Ok(new UserDto
        {
            Id = user.Id!,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin
        });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var user = await _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null) return NotFound(new { message = "Usuario no encontrado" });

        // Don't delete the last admin
        if (user.Role == "Admin")
        {
            var adminCount = await _db.Users.CountDocumentsAsync(u => u.Role == "Admin" && u.IsActive);
            if (adminCount <= 1)
            {
                return BadRequest(new { message = "No se puede eliminar el único administrador del sistema." });
            }
        }

        user.IsActive = false;
        await _db.Users.ReplaceOneAsync(u => u.Id == id, user);

        return Ok(new { message = "Usuario desactivado exitosamente" });
    }
}
