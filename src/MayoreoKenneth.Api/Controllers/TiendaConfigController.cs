using MayoreoKenneth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MayoreoKenneth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TiendaConfigController : ControllerBase
{
    private readonly MayoreoKennethDbContext _db;

    public TiendaConfigController(MayoreoKennethDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerAsync(CancellationToken cancellationToken)
    {
        // Yurguen: Solo Admin ajusta margen, IVA y días sin stock antes de ocultar.
        var s = await _db.StoreSettings.AsNoTracking().FirstAsync(cancellationToken);
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
    public async Task<IActionResult> ActualizarAsync([FromBody] ActualizarTiendaConfigRequest body, CancellationToken cancellationToken)
    {
        var s = await _db.StoreSettings.FirstAsync(cancellationToken);
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
