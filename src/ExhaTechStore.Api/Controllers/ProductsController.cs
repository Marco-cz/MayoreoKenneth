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
                x.ImageUrl,
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
    [ResponseCache(
        Duration = 20,
        Location = ResponseCacheLocation.Any,
        VaryByHeader = "Accept-Encoding",
        VaryByQueryKeys = new[] { "page", "pageSize", "q" })]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery] string? q = null,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        // Yurguen: Paginación grande por defecto para catálogos de muchos ítems.
        pageSize = pageSize is < 1 or > 96 ? 48 : pageSize;
        var queryText = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            var all = InMemoryListCache.Value.AsEnumerable();
            if (queryText is not null)
            {
                var qt = queryText.ToLowerInvariant();
                all = all.Where(x =>
                    x.Name.ToLowerInvariant().Contains(qt) ||
                    x.Sku.ToLowerInvariant().Contains(qt));
            }

            var filtered = all.OrderBy(x => x.Name).ToList();
            var total = filtered.Count;
            var slice = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var hasMore = page * pageSize < total;
            return Ok(new ProductCatalogPageResponse(slice, total, page, pageSize, hasMore));
        }

        var baseQuery = _dbContext.Products
            .AsNoTracking()
            .Where(x => x.TenantId == _tenant.TenantId && x.IsPublished && !x.IsCatalogHidden);

        if (queryText is not null)
        {
            var term = queryText.ToLowerInvariant();
            baseQuery = baseQuery.Where(x =>
                x.Name.ToLower().Contains(term) || x.Sku.ToLower().Contains(term));
        }

        var totalDb = await baseQuery.CountAsync(cancellationToken);

        var rows = await baseQuery
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductListItemResponse(
                x.Id,
                x.Sku,
                x.Name,
                x.ImageUrl,
                x.DisplayPriceWithIva,
                x.IsPublished,
                x.IvaIncludedInDisplayPrice,
                x.UpdatedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var hasMoreDb = page * pageSize < totalDb;
        return Ok(new ProductCatalogPageResponse(rows, totalDb, page, pageSize, hasMoreDb));
    }

    // Yurguen: Base para "Más vendidos" (últimos N días) a partir de órdenes confirmadas/entregadas.
    [HttpGet("top-selling")]
    public async Task<IActionResult> GetTopSellingAsync(
        [FromQuery] int limit = 12,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        limit = limit is < 1 or > 48 ? 12 : limit;
        days = days is < 1 or > 365 ? 30 : days;

        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            // Yurguen: En demo memoria aún no hay ventas persistidas; aproximamos con mayor stock simulado.
            var topMem = _catalogStore.GetProducts()
                .OrderByDescending(x => x.SimulatedStock)
                .ThenBy(x => x.Name)
                .Take(limit)
                .Select(x => new ProductListItemResponse(
                    x.Id,
                    x.Sku,
                    x.Name,
                    x.ImageUrl,
                    x.Price,
                    x.IsPublished,
                    true,
                    DateTime.UtcNow))
                .ToList();
            return Ok(topMem);
        }

        var sinceUtc = DateTime.UtcNow.AddDays(-days);
        var closedStatuses = new[] { OrderStatus.Confirmed, OrderStatus.Delivered };

        var topDb = await _dbContext.OrderItems
            .AsNoTracking()
            .Where(oi =>
                oi.Order.TenantId == _tenant.TenantId &&
                oi.Product.TenantId == _tenant.TenantId &&
                oi.Product.IsPublished &&
                !oi.Product.IsCatalogHidden &&
                oi.Order.CreatedAtUtc >= sinceUtc &&
                closedStatuses.Contains(oi.Order.Status))
            .GroupBy(oi => new
            {
                oi.Product.Id,
                oi.Product.Sku,
                oi.Product.Name,
                oi.Product.ImageUrl,
                oi.Product.DisplayPriceWithIva,
                oi.Product.IvaIncludedInDisplayPrice,
                LastUpdated = oi.Product.UpdatedAtUtc ?? oi.Product.CreatedAtUtc
            })
            .Select(g => new
            {
                Product = new ProductListItemResponse(
                    g.Key.Id,
                    g.Key.Sku,
                    g.Key.Name,
                    g.Key.ImageUrl,
                    g.Key.DisplayPriceWithIva,
                    true,
                    g.Key.IvaIncludedInDisplayPrice,
                    g.Key.LastUpdated),
                Qty = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.Qty)
            .ThenBy(x => x.Product.Name)
            .Take(limit)
            .Select(x => x.Product)
            .ToListAsync(cancellationToken);

        return Ok(topDb);
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
            item.ImageUrl,
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
            // Yurguen: alinear con ProductDetailResponse (detalle público incluye foto).
            entity.ImageUrl,
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

public sealed record ProductCatalogPageResponse(
    IReadOnlyList<ProductListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record ProductListItemResponse(
    Guid Id,
    string Sku,
    string Name,
    string? ImageUrl,
    decimal Price,
    bool IsPublished,
    bool IvaIncluidoEnPrecio,
    DateTime LastUpdatedAtUtc);

public sealed record ProductDetailResponse(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    string? ImageUrl,
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
