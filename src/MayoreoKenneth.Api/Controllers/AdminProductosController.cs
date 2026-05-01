using MayoreoKenneth.Infrastructure.Persistence;
using MayoreoKenneth.Infrastructure.Pricing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MayoreoKenneth.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class ProductosController : ControllerBase
{
    private readonly MayoreoKennethDbContext _db;

    public ProductosController(MayoreoKennethDbContext db)
    {
        _db = db;
    }

    [HttpPatch("{id:guid}/descuento")]
    public async Task<IActionResult> ActualizarDescuentoAsync(
        Guid id,
        [FromBody] ActualizarDescuentoRequest body,
        CancellationToken cancellationToken)
    {
        // Yurguen: Descuento admin por producto; el precio con IVA se recalcula con costo y margen globales.
        var product = await _db.Products
            .Include(x => x.SupplierMappings)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound(new { message = "Producto no encontrado." });
        }

        var pct = Math.Clamp(body.PorcientoDescuento, 0m, 100m);
        product.AdminDiscountPercent = pct;
        product.UpdatedAtUtc = DateTime.UtcNow;

        var settings = await _db.StoreSettings.AsNoTracking().FirstAsync(cancellationToken);
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
