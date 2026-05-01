using MayoreoKenneth.Api.Features.Catalog;
using MayoreoKenneth.Domain.Entities;
using MayoreoKenneth.Infrastructure.CatalogSync;
using MayoreoKenneth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MayoreoKenneth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly MayoreoKennethDbContext _dbContext;
    private readonly InMemoryCatalogStore _catalogStore;
    private readonly IConfiguration _configuration;
    private readonly IExternalLiveStockSource _liveStockSource;

    public ProductsController(
        MayoreoKennethDbContext dbContext,
        InMemoryCatalogStore catalogStore,
        IConfiguration configuration,
        IExternalLiveStockSource liveStockSource)
    {
        _dbContext = dbContext;
        _catalogStore = catalogStore;
        _configuration = configuration;
        _liveStockSource = liveStockSource;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        // Yurguen: En modo local sin BD usamos seed en memoria para avanzar rapido.
        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            // Yurguen: Listado sin stock; la disponibilidad se consulta en detalle / al comprar.
            var inMemoryProducts = _catalogStore.GetProducts()
                .Select(x => new ProductListItemResponse(
                    x.Id,
                    x.Sku,
                    x.Name,
                    x.Price,
                    x.IsPublished,
                    true,
                    DateTime.UtcNow))
                .ToList();

            return Ok(inMemoryProducts);
        }

        // Yurguen: Catálogo desde BD: solo publicados y no ocultos por stock prolongado.
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsPublished && !x.IsCatalogHidden)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var response = products
            .Select(x => new ProductListItemResponse(
                x.Id,
                x.Sku,
                x.Name,
                x.DisplayPriceWithIva,
                x.IsPublished,
                x.IvaIncludedInDisplayPrice,
                x.UpdatedAtUtc ?? x.CreatedAtUtc))
            .ToList();

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // Yurguen: Endpoint de detalle para consumir desde la web.
        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            var item = _catalogStore.GetById(id);
            if (item is null)
            {
                return NotFound();
            }

            var liveOutcome = await TryLiveStockForMemoryAsync(item, cancellationToken);
            return Ok(BuildDetailFromMemory(item, liveOutcome));
        }

        var entity = await _dbContext.Products
            .AsNoTracking()
            .Include(x => x.SupplierMappings)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished && !x.IsCatalogHidden, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        var mapping = entity.SupplierMappings.FirstOrDefault(m => m.IsActive) ?? entity.SupplierMappings.FirstOrDefault();
        var liveOutcomeDb = await TryLiveStockForMappingAsync(mapping, cancellationToken);
        return Ok(BuildDetailFromEntity(entity, liveOutcomeDb));
    }

    // Yurguen: Solo stock/precio consulta al proveedor; ideal para refrescar sin cargar todo el detalle.
    [HttpGet("{id:guid}/stock-en-vivo")]
    public async Task<IActionResult> GetStockEnVivoAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            var item = _catalogStore.GetById(id);
            if (item is null)
            {
                return NotFound();
            }

            var liveOutcomeMem = await TryLiveStockForMemoryAsync(item, cancellationToken);
            return Ok(ToStockEnVivoResponse(liveOutcomeMem));
        }

        var entity = await _dbContext.Products
            .AsNoTracking()
            .Include(x => x.SupplierMappings)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished && !x.IsCatalogHidden, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        var mapping = entity.SupplierMappings.FirstOrDefault(m => m.IsActive) ?? entity.SupplierMappings.FirstOrDefault();
        var liveOutcomeDb2 = await TryLiveStockForMappingAsync(mapping, cancellationToken);
        return Ok(ToStockEnVivoResponse(liveOutcomeDb2));
    }

    private bool LiveStockEnabled() =>
        _configuration.GetValue("Catalog:LiveStockOnProductDetailEnabled", true);

    private async Task<ExternalLiveStockOutcome?> TryLiveStockForMemoryAsync(
        CatalogProduct item,
        CancellationToken cancellationToken)
    {
        if (!LiveStockEnabled())
        {
            return null;
        }

        return await _liveStockSource.QueryLiveStockAsync(
            new ExternalLiveStockQuery(null, item.Sku),
            cancellationToken);
    }

    private async Task<ExternalLiveStockOutcome?> TryLiveStockForMappingAsync(
        ProductSupplierMap? mapping,
        CancellationToken cancellationToken)
    {
        if (!LiveStockEnabled() || mapping is null)
        {
            return null;
        }

        return await _liveStockSource.QueryLiveStockAsync(
            new ExternalLiveStockQuery(mapping.SupplierProductId, mapping.SupplierSku),
            cancellationToken);
    }

    private static ProductDetailResponse BuildDetailFromMemory(CatalogProduct item, ExternalLiveStockOutcome? live)
    {
        var (enVivo, utc, ok, err) = MapLive(live);
        return new ProductDetailResponse(
            item.Id,
            item.Sku,
            item.Name,
            item.Description,
            item.Price,
            item.IsPublished,
            true,
            enVivo,
            utc,
            ok,
            err);
    }

    private static ProductDetailResponse BuildDetailFromEntity(
        Product entity,
        ExternalLiveStockOutcome? live)
    {
        var (enVivo, utc, ok, err) = MapLive(live);
        return new ProductDetailResponse(
            entity.Id,
            entity.Sku,
            entity.Name,
            entity.Description ?? string.Empty,
            entity.DisplayPriceWithIva,
            entity.IsPublished,
            entity.IvaIncludedInDisplayPrice,
            enVivo,
            utc,
            ok,
            err);
    }

    private static (int? EnVivo, DateTime? Utc, bool Ok, string? Error) MapLive(ExternalLiveStockOutcome? live)
    {
        if (live is null)
        {
            return (null, null, false, null);
        }

        if (!live.SupplierResponded)
        {
            return (null, live.CheckedAtUtc, false, live.ErrorMessage ?? "Proveedor no respondio.");
        }

        if (!live.ProductFound)
        {
            return (null, live.CheckedAtUtc, false, "Producto no encontrado en proveedor.");
        }

        return (live.Quantity, live.CheckedAtUtc, true, null);
    }

    private static ProductStockEnVivoResponse ToStockEnVivoResponse(ExternalLiveStockOutcome? live)
    {
        var (enVivo, utc, ok, err) = MapLive(live);
        return new ProductStockEnVivoResponse(enVivo, utc, ok, err);
    }
}

public sealed record ProductListItemResponse(
    Guid Id,
    string Sku,
    string Name,
    decimal Price,
    bool IsPublished,
    bool IvaIncluidoEnPrecio,
    DateTime LastUpdatedAtUtc);

public sealed record ProductDetailResponse(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    decimal Price,
    bool IsPublished,
    bool IvaIncluidoEnPrecio,
    int? StockEnVivo,
    DateTime? StockEnVivoConsultadoUtc,
    bool StockEnVivoConsultaOk,
    string? StockEnVivoMensaje);

public sealed record ProductStockEnVivoResponse(
    int? StockEnVivo,
    DateTime? ConsultadoUtc,
    bool ConsultaOk,
    string? Mensaje);
