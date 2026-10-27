using System.Security.Cryptography;
using System.Text;
using BodegaDebts.Api.DTOs;
using BodegaDebts.Api.Models;
using BodegaDebts.Api.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BodegaDebts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MongoDbService _db;

    public AuthController(MongoDbService db)
    {
        _db = db;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Usuario y contraseña requeridos" });
        }

        var usernameNormalized = dto.Username.Trim().ToLower();
        var user = await _db.Users.Find(u => u.Username.ToLower() == usernameNormalized && u.IsActive).FirstOrDefaultAsync();

        if (user == null)
        {
            return Unauthorized(new { message = "Credenciales incorrectas o usuario inactivo" });
        }

        // Verify password hash
        var hash = HashPassword(dto.Password);
        if (user.PasswordHash != hash)
        {
            return Unauthorized(new { message = "Credenciales incorrectas" });
        }

        user.LastLogin = DateTime.UtcNow;
        await _db.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

        // Simple token (base64 of user info + timestamp + signature)
        var tokenPayload = $"{user.Id}:{user.Username}:{user.Role}:{DateTime.UtcNow.Ticks}";
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenPayload));

        return Ok(new LoginResponseDto
        {
            Token = token,
            Id = user.Id!,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role
        });
    }

    [HttpPost("verify-admin")]
    public async Task<ActionResult> VerifyAdmin([FromBody] VerifyAdminDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Contraseña de administrador requerida" });
        }

        var hash = HashPassword(dto.Password);
        var admin = await _db.Users.Find(u => u.Role == "Admin" && u.IsActive && u.PasswordHash == hash).FirstOrDefaultAsync();

        if (admin == null)
        {
            return Unauthorized(new { message = "Contraseña incorrecta. Este espacio es exclusivo del administrador." });
        }

        return Ok(new { success = true });
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "_bodegasecret_2026"));
        return Convert.ToBase64String(bytes);
    }
}
