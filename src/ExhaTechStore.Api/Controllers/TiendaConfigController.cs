using ExhaTechStore.Api.Tenancy;
using ExhaTechStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExhaTechStore.Api.Controllers;

// Yurguen: Config por tenant (/t/{tenantSlug}/api/tiendaconfig).
[ApiController]
[Route("t/{tenantSlug}/api/[controller]")]
[ServiceFilter(typeof(TenantSlugResolutionFilter))]
[Authorize(Roles = "Admin")]
public class TiendaConfigController : ControllerBase
{
    private readonly ExhaTechStoreDbContext _db;
    private readonly TenantContext _tenant;

    public TiendaConfigController(ExhaTechStoreDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerAsync(CancellationToken cancellationToken)
    {
        var s = await _db.StoreSettings.AsNoTracking()
            .FirstAsync(x => x.TenantId == _tenant.TenantId, cancellationToken);
        return Ok(new
        {
            margenPorDefectoPorciento = s.DefaultMarkupPercent,
            ivaPorciento = s.IvaPercentOnSale,
            diasSinStockParaOcultar = s.UnavailableHideAfterDays,
            horaSyncCatalogoLocal = s.CatalogSyncHourLocal,
            zonaHoraria = s.TimeZoneId,
            ultimaSyncCatalogoUtc = s.LastCatalogSyncCompletedAtUtc
        });
    }

    [HttpPut]
    public async Task<IActionResult> ActualizarAsync(
        [FromBody] ActualizarTiendaConfigRequest body,
        CancellationToken cancellationToken)
    {
        var s = await _db.StoreSettings.FirstAsync(x => x.TenantId == _tenant.TenantId, cancellationToken);
        if (body.MargenPorDefectoPorciento is not null)
        {
            s.DefaultMarkupPercent = Math.Clamp(body.MargenPorDefectoPorciento.Value, 0m, 500m);
        }

        if (body.IvaPorciento is not null)
        {
            s.IvaPercentOnSale = Math.Clamp(body.IvaPorciento.Value, 0m, 100m);
        }

        if (body.DiasSinStockParaOcultar is not null)
        {
            s.UnavailableHideAfterDays = Math.Clamp(body.DiasSinStockParaOcultar.Value, 1, 365);
        }

        if (body.HoraSyncCatalogoLocal is not null)
        {
            s.CatalogSyncHourLocal = Math.Clamp(body.HoraSyncCatalogoLocal.Value, 0, 23);
        }

        if (!string.IsNullOrWhiteSpace(body.ZonaHoraria))
        {
            s.TimeZoneId = body.ZonaHoraria.Trim();
        }

        s.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Configuración actualizada." });
    }
}

public sealed class ActualizarTiendaConfigRequest
{
    public decimal? MargenPorDefectoPorciento { get; set; }
    public decimal? IvaPorciento { get; set; }
    public int? DiasSinStockParaOcultar { get; set; }
    public int? HoraSyncCatalogoLocal { get; set; }
    public string? ZonaHoraria { get; set; }
}
