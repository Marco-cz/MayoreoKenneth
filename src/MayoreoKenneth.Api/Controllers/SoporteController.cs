using MayoreoKenneth.Api.Features.Soporte;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MayoreoKenneth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SoporteController : ControllerBase
{
    private readonly InMemorySupportTicketsStore _store;

    public SoporteController(InMemorySupportTicketsStore store)
    {
        _store = store;
    }

    [HttpPost("reportar")]
    [AllowAnonymous]
    public IActionResult Reportar([FromBody] ReportarProblemaRequest request)
    {
        // Yurguen: Cualquier visitante puede reportar; mensajes en español para usuario final.
        if (string.IsNullOrWhiteSpace(request.Correo) || !request.Correo.Contains('@', StringComparison.Ordinal))
        {
            return BadRequest(new { message = "Indique un correo electrónico válido." });
        }

        if (string.IsNullOrWhiteSpace(request.NombreContacto))
        {
            return BadRequest(new { message = "El nombre de contacto es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(request.Asunto) || request.Asunto.Trim().Length < 3)
        {
            return BadRequest(new { message = "El asunto debe tener al menos 3 caracteres." });
        }

        if (string.IsNullOrWhiteSpace(request.Descripcion) || request.Descripcion.Trim().Length < 10)
        {
            return BadRequest(new { message = "Escriba su mensaje con al menos 10 caracteres." });
        }

        var caso = _store.Agregar(
            request.Correo,
            request.NombreContacto,
            request.NumeroOrden,
            request.Asunto,
            request.Descripcion);

        return Ok(new
        {
            message = "Su mensaje fue registrado (error, comentario o consulta). Nos pondremos en contacto pronto.",
            id = caso.Id
        });
    }

    [HttpGet("casos")]
    [Authorize(Roles = "Admin,Soporte")]
    public IActionResult ListarCasos()
    {
        // Yurguen: Admin o Soporte ven casos activos (borrado logico oculta resueltos).
        return Ok(_store.ListarActivos());
    }

    [HttpPost("casos/{id:guid}/resolver")]
    [Authorize(Roles = "Admin,Soporte")]
    public IActionResult ResolverCaso(Guid id)
    {
        // Yurguen: Borrado logico — el caso deja de aparecer en la bandeja pero permanece almacenado.
        var ok = _store.MarcarResuelto(id);
        if (!ok)
        {
            return NotFound(new { message = "No se encontró el caso o ya estaba resuelto." });
        }

        return Ok(new { message = "Caso marcado como resuelto." });
    }
}

public sealed record ReportarProblemaRequest(
    string Correo,
    string NombreContacto,
    string? NumeroOrden,
    string Asunto,
    string Descripcion);
