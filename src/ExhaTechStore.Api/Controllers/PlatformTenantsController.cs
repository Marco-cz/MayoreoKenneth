using ExhaTechStore.Api.Tenancy;
using ExhaTechStore.Domain.Entities;
using ExhaTechStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExhaTechStore.Api.Controllers;

// Yurguen: Alta y mantenimiento global de clientes (tenants); sin segmento /t/{slug}/.
[ApiController]
[Route("api/platform/[controller]")]
[Authorize(Roles = "PlatformOwner")]
public class PlatformTenantsController : ControllerBase
{
    private readonly ExhaTechStoreDbContext _db;
    private readonly IConfiguration _configuration;

    public PlatformTenantsController(ExhaTechStoreDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            var dev = _configuration.GetSection("Catalog:DevTenants").Get<List<DevTenantBinding>>() ?? [];
            var dto = dev
                .Where(x => !string.IsNullOrWhiteSpace(x.Id) && Guid.TryParse(x.Id, out _))
                .Select(x => new TenantListItemDto(
                    Guid.Parse(x.Id!),
                    x.Slug.Trim().ToLowerInvariant(),
                    string.IsNullOrWhiteSpace(x.DisplayName) ? x.Slug : x.DisplayName,
                    true))
                .ToList();
            return Ok(new { modo = "inMemory", tenants = dto });
        }

        var rows = await _db.Tenants.AsNoTracking()
            .OrderBy(t => t.Slug)
            .Select(t => new TenantListItemDto(t.Id, t.Slug, t.DisplayName, t.IsActive))
            .ToListAsync(ct);

        return Ok(new { modo = "database", tenants = rows });
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearTenantRequest body, CancellationToken ct)
    {
        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            return Conflict(new
            {
                message =
                    "Yurguen: Con Catalog:UseInMemory=true no se guardan tenants en BD. Poné Postgres y UseInMemory=false para dar de alta clientes."
            });
        }

        var slug = (body.Slug ?? "").Trim().ToLowerInvariant();
        var nombre = (body.DisplayName ?? "").Trim();
        if (slug.Length == 0 || nombre.Length == 0)
        {
            return BadRequest(new { message = "Slug y nombre para mostrar son obligatorios." });
        }

        if (!SlugValido(slug))
        {
            return BadRequest(new { message = "Slug inválido: use minúsculas, números y guiones." });
        }

        var existe = await _db.Tenants.AnyAsync(t => t.Slug == slug, ct);
        if (existe)
        {
            return Conflict(new { message = "Ya existe un tenant con ese slug." });
        }

        var ent = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            DisplayName = nombre,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Tenants.Add(ent);
        await _db.SaveChangesAsync(ct);

        var dto = new TenantListItemDto(ent.Id, ent.Slug, ent.DisplayName, ent.IsActive);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpPatch("{id:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoTenantRequest body, CancellationToken ct)
    {
        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            return Conflict(new { message = "Yurguen: En modo memoria no hay persistencia para activar/desactivar tenants." });
        }

        var ent = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ent is null)
        {
            return NotFound(new { message = "Tenant no encontrado." });
        }

        ent.IsActive = body.Activo;
        await _db.SaveChangesAsync(ct);
        return Ok(new TenantListItemDto(ent.Id, ent.Slug, ent.DisplayName, ent.IsActive));
    }

    private static bool SlugValido(string s)
    {
        foreach (var c in s)
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')
            {
                continue;
            }

            return false;
        }

        return s.Length > 0 && s[0] != '-' && s[^1] != '-';
    }
}

public sealed record TenantListItemDto(Guid Id, string Slug, string DisplayName, bool IsActive);

public sealed record CrearTenantRequest(string Slug, string DisplayName);

public sealed record CambiarEstadoTenantRequest(bool Activo);
