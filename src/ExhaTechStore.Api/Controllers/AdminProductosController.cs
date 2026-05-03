using ExhaTechStore.Api.Tenancy;
using ExhaTechStore.Infrastructure.Persistence;
using ExhaTechStore.Infrastructure.Pricing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExhaTechStore.Api.Controllers;

// Yurguen: Admin productos por tenant.
[ApiController]
[Route("t/{tenantSlug}/api/admin/[controller]")]
[ServiceFilter(typeof(TenantSlugResolutionFilter))]
[Authorize(Roles = "Admin")]
public class ProductosController : ControllerBase
{
    private readonly ExhaTechStoreDbContext _db;
    private readonly TenantContext _tenant;

    public ProductosController(ExhaTechStoreDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpPatch("{id:guid}/descuento")]
    public async Task<IActionResult> ActualizarDescuentoAsync(
        Guid id,
        [FromBody] ActualizarDescuentoRequest body,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .Include(x => x.SupplierMappings)
            .FirstOrDefaultAsync(
                x => x.Id == id && x.TenantId == _tenant.TenantId,
                cancellationToken);

        if (product is null)
        {
            return NotFound(new { message = "Producto no encontrado." });
        }

        var pct = Math.Clamp(body.PorcientoDescuento, 0m, 100m);
        product.AdminDiscountPercent = pct;
        product.UpdatedAtUtc = DateTime.UtcNow;

        var settings = await _db.StoreSettings.AsNoTracking()
            .FirstAsync(x => x.TenantId == _tenant.TenantId, cancellationToken);
        var map = product.SupplierMappings.FirstOrDefault();
        if (map is not null)
        {
            product.DisplayPriceWithIva = DisplayPriceCalculator.ComputeDisplayPriceWithIva(
                map.LastKnownCost,
                settings.DefaultMarkupPercent,
                settings.IvaPercentOnSale,
                product.AdminDiscountPercent);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Descuento actualizado.", porcientoDescuento = pct, precioConIva = product.DisplayPriceWithIva });
    }
}

public sealed record ActualizarDescuentoRequest(decimal PorcientoDescuento);
