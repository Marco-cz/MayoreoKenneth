using ExhaTechStore.Api.Features.Catalog;
using ExhaTechStore.Api.Tenancy;
using ExhaTechStore.Domain.Entities;
using ExhaTechStore.Domain.Enums;
using ExhaTechStore.Infrastructure.CatalogSync;
using ExhaTechStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExhaTechStore.Api.Controllers;

// Yurguen: Catálogo público por tenant: /t/{tenantSlug}/api/products
[ApiController]
[Route("t/{tenantSlug}/api/[controller]")]
[ServiceFilter(typeof(TenantSlugResolutionFilter))]
public class ProductsController : ControllerBase
{
    // Yurguen: Proyección MVP en memoria precomputada (evita alocar lista en cada GET).
    private static readonly Lazy<IReadOnlyList<ProductListItemResponse>> InMemoryListCache = new(() =>
        new InMemoryCatalogStore().GetProducts()
            .Select(x => new ProductListItemResponse(
                x.Id,
                x.Sku,
                x.Name,
                x.Price,
                x.IsPublished,
                true,
                DateTime.UtcNow))
            .ToList());

    private readonly ExhaTechStoreDbContext _dbContext;
    private readonly InMemoryCatalogStore _catalogStore;
    private readonly IConfiguration _configuration;
    private readonly IExternalLiveStockSource _liveStockSource;
    private readonly TenantContext _tenant;

    public ProductsController(
        ExhaTechStoreDbContext dbContext,
        InMemoryCatalogStore catalogStore,
        IConfiguration configuration,
        IExternalLiveStockSource liveStockSource,
        TenantContext tenant)
    {
        _dbContext = dbContext;
        _catalogStore = catalogStore;
        _configuration = configuration;
        _liveStockSource = liveStockSource;
        _tenant = tenant;
    }

    [HttpGet]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            return Ok(InMemoryListCache.Value);
        }

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.TenantId == _tenant.TenantId && x.IsPublished && !x.IsCatalogHidden)
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
            .FirstOrDefaultAsync(
                x => x.Id == id && x.TenantId == _tenant.TenantId && x.IsPublished && !x.IsCatalogHidden,
                cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        var mapping = entity.SupplierMappings.FirstOrDefault(m => m.IsActive) ?? entity.SupplierMappings.FirstOrDefault();
        var liveOutcomeDb = await ResolveStockOutcomeAsync(entity, mapping, cancellationToken);
        return Ok(BuildDetailFromEntity(entity, liveOutcomeDb));
    }

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
            .FirstOrDefaultAsync(
                x => x.Id == id && x.TenantId == _tenant.TenantId && x.IsPublished && !x.IsCatalogHidden,
                cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        var mapping = entity.SupplierMappings.FirstOrDefault(m => m.IsActive) ?? entity.SupplierMappings.FirstOrDefault();
        var liveOutcomeDb2 = await ResolveStockOutcomeAsync(entity, mapping, cancellationToken);
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
            _tenant.TenantId,
            new ExternalLiveStockQuery(null, item.Sku),
            cancellationToken);
    }

    // Yurguen: SupplierManaged → proveedor en vivo; TenantManaged/Hybrid → ManualStockQuantity.
    private async Task<ExternalLiveStockOutcome?> ResolveStockOutcomeAsync(
        Product entity,
        ProductSupplierMap? mapping,
        CancellationToken cancellationToken)
    {
        if (entity.InventoryMode is ProductInventoryMode.TenantManaged or ProductInventoryMode.Hybrid)
        {
            var utc = DateTime.UtcNow;
            var qty = Math.Max(0, entity.ManualStockQuantity);
            return new ExternalLiveStockOutcome(true, true, qty, utc, null);
        }

        if (!LiveStockEnabled() || mapping is null)
        {
            return null;
        }

        return await _liveStockSource.QueryLiveStockAsync(
            _tenant.TenantId,
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
            nameof(ProductInventoryMode.SupplierManaged),
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
        var modo = Enum.GetName(entity.InventoryMode) ?? entity.InventoryMode.ToString();
        return new ProductDetailResponse(
            entity.Id,
            entity.Sku,
            entity.Name,
            entity.Description ?? string.Empty,
            entity.DisplayPriceWithIva,
            entity.IsPublished,
            entity.IvaIncludedInDisplayPrice,
            modo,
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
    /// <summary>Yurguen: SupplierManaged | TenantManaged | Hybrid.</summary>
    string ModoInventario,
    int? StockEnVivo,
    DateTime? StockEnVivoConsultadoUtc,
    bool StockEnVivoConsultaOk,
    string? StockEnVivoMensaje);

public sealed record ProductStockEnVivoResponse(
    int? StockEnVivo,
    DateTime? ConsultadoUtc,
    bool ConsultaOk,
    string? Mensaje);
