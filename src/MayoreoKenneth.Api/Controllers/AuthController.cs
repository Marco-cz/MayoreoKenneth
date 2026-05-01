using System.Security.Claims;
using MayoreoKenneth.Api.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MayoreoKenneth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly InMemoryAuthStore _authStore;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(InMemoryAuthStore authStore, JwtTokenService jwtTokenService)
    {
        _authStore = authStore;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("iniciar-sesion")]
    [AllowAnonymous]
    public IActionResult IniciarSesion([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Correo y contraseña son obligatorios." });
        }

        var user = _authStore.ValidateCredentials(request.Email, request.Password);
        if (user is null)
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        var token = _jwtTokenService.GenerateToken(user);
        return Ok(new LoginResponse(token, user.FullName, user.Role, user.Email));
    }

    [HttpGet("perfil")]
    [Authorize]
    public IActionResult Perfil()
    {
        return Ok(new
        {
            nombre = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            correo = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            rol = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty
        });
    }

    [HttpGet("solo-admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult SoloAdmin()
    {
        return Ok(new { message = "Acceso autorizado para rol Administrador." });
    }

    [HttpGet("solo-soporte")]
    [Authorize(Roles = "Soporte")]
    public IActionResult SoloSoporte()
    {
        // Yurguen: Prueba de rol Soporte (bandeja de casos); sin permisos de precios/admin.
        return Ok(new { message = "Acceso autorizado para rol Soporte." });
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string Token, string FullName, string Role, string Email);
