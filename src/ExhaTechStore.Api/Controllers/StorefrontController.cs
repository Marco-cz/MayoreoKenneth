using ExhaTechStore.Api.Tenancy;
using ExhaTechStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExhaTechStore.Api.Controllers;

// Yurguen: Datos públicos de marca por tenant (consumo tienda web sin login).
[ApiController]
[Route("t/{tenantSlug}/api/[controller]")]
[ServiceFilter(typeof(TenantSlugResolutionFilter))]
public class StorefrontController : ControllerBase
{
    private readonly TenantContext _tenant;
    private readonly IConfiguration _configuration;
    private readonly ExhaTechStoreDbContext _db;

    public StorefrontController(
        TenantContext tenant,
        IConfiguration configuration,
        ExhaTechStoreDbContext db)
    {
        _tenant = tenant;
        _configuration = configuration;
        _db = db;
    }

    /// <summary>Yurguen: slug, nombre visible, logo opcional, tel pie opcional (camelCase JSON).</summary>
    [HttpGet("perfil")]
    [AllowAnonymous]
    public async Task<IActionResult> PerfilAsync(CancellationToken cancellationToken)
    {
        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            var devTenants =
                _configuration.GetSection("Catalog:DevTenants").Get<List<DevTenantBinding>>() ?? [];
            var match = devTenants.FirstOrDefault(t =>
                string.Equals(t.Slug.Trim(), _tenant.Slug, StringComparison.OrdinalIgnoreCase));
            var displayName = string.IsNullOrWhiteSpace(match?.DisplayName)
                ? _tenant.Slug
                : match!.DisplayName!.Trim();
            // Yurguen: En modo memoria, usamos DevTenants para pie personalizable por tienda.
            var footerPhone = string.IsNullOrWhiteSpace(match?.FooterPhone) ? null : match!.FooterPhone!.Trim();
            return Ok(new StorefrontPerfilResponse(_tenant.Slug, displayName, null, footerPhone));
        }

        var row = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == _tenant.TenantId)
            .Select(t => new { t.DisplayName, t.LogoUrl, t.FooterPhone })
            .FirstAsync(cancellationToken);

        return Ok(new StorefrontPerfilResponse(
            _tenant.Slug,
            row.DisplayName,
            row.LogoUrl,
            row.FooterPhone));
    }

    /// <summary>Yurguen: Admin cambia logo y tel pie; requiere BD (no modo InMemory).</summary>
    [HttpPatch("marca")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActualizarMarcaAsync(
        [FromBody] ActualizarStorefrontMarcaDto body,
        CancellationToken cancellationToken)
    {
        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            return BadRequest(new { message = "Logo y pie: activá Postgres (UseInMemory false) y migraciones." });
        }

        var tenant = await _db.Tenants.FirstAsync(t => t.Id == _tenant.TenantId, cancellationToken);
        if (body.LogoUrl is not null)
        {
            tenant.LogoUrl = string.IsNullOrWhiteSpace(body.LogoUrl) ? null : body.LogoUrl.Trim();
        }

        if (body.FooterPhone is not null)
        {
            tenant.FooterPhone = string.IsNullOrWhiteSpace(body.FooterPhone) ? null : body.FooterPhone.Trim();
        }

        if (!string.IsNullOrWhiteSpace(body.DisplayName))
        {
            tenant.DisplayName = body.DisplayName.Trim();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new
        {
            message = "Marca de tienda actualizada.",
            logoUrl = tenant.LogoUrl,
            footerPhone = tenant.FooterPhone,
            displayName = tenant.DisplayName
        });
    }
}

public sealed record StorefrontPerfilResponse(
    string Slug,
    string DisplayName,
    string? LogoUrl,
    string? FooterPhone);

public sealed class ActualizarStorefrontMarcaDto
{
    public string? LogoUrl { get; set; }
    public string? FooterPhone { get; set; }

    /// <summary>Yurguen: Nombre visible en cabecera tienda opcional.</summary>
    public string? DisplayName { get; set; }
}
