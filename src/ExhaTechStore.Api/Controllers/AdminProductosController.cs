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

    // Yurguen: Nombre, descripción y código (SKU) visibles en la tienda; SKU único por tenant.
    [HttpPatch("{id:guid}/texto-catalogo")]
    public async Task<IActionResult> ActualizarTextoCatalogoAsync(
        Guid id,
        [FromBody] ActualizarProductoCatalogoTextoDto body,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(
            x => x.Id == id && x.TenantId == _tenant.TenantId,
            cancellationToken);

        if (product is null)
        {
            return NotFound(new { message = "Producto no encontrado." });
        }

        var sku = body.Sku?.Trim();
        var nombre = body.Nombre?.Trim();
        if (string.IsNullOrEmpty(sku) || string.IsNullOrEmpty(nombre))
        {
            return BadRequest(new { message = "Sku y nombre son obligatorios." });
        }

        var duplicado = await _db.Products.AsNoTracking()
            .AnyAsync(x => x.TenantId == _tenant.TenantId && x.Id != id && x.Sku == sku,
                cancellationToken);
        if (duplicado)
        {
            return BadRequest(new { message = "Ya existe otro producto con ese SKU en esta tienda." });
        }

        product.Sku = sku;
        product.Name = nombre;
        product.Description = string.IsNullOrWhiteSpace(body.Descripcion)
            ? null
            : body.Descripcion.Trim();
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Texto de catálogo actualizado.", sku, nombre, descripcion = product.Description });
    }
}

public sealed record ActualizarDescuentoRequest(decimal PorcientoDescuento);

public sealed class ActualizarProductoCatalogoTextoDto
{
    public string? Sku { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
}
