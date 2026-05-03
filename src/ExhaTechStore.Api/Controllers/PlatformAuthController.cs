using ExhaTechStore.Api.Features.Auth;
using ExhaTechStore.Api.Features.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhaTechStore.Api.Controllers;

// Yurguen: Login separado para dueños; JWT con rol PlatformOwner (rutas /api/platform/...).
[ApiController]
[Route("api/platform/auth")]
public class PlatformAuthController : ControllerBase
{
    private readonly InMemoryPlatformAuthStore _platformAuth;
    private readonly JwtTokenService _jwtTokenService;

    public PlatformAuthController(InMemoryPlatformAuthStore platformAuth, JwtTokenService jwtTokenService)
    {
        _platformAuth = platformAuth;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("iniciar-sesion")]
    [AllowAnonymous]
    public IActionResult IniciarSesion([FromBody] PlatformLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Correo y contraseña son obligatorios." });
        }

        var user = _platformAuth.ValidateOwner(request.Email, request.Password);
        if (user is null)
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        var token = _jwtTokenService.GenerateToken(user);
        return Ok(new PlatformLoginResponse(token, user.FullName, user.Role, user.Email));
    }
}

public sealed record PlatformLoginRequest(string Email, string Password);

public sealed record PlatformLoginResponse(string Token, string FullName, string Role, string Email);
