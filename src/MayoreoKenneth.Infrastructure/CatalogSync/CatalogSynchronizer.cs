using MayoreoKenneth.Domain.Entities;
using MayoreoKenneth.Infrastructure.Persistence;
using MayoreoKenneth.Infrastructure.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MayoreoKenneth.Infrastructure.CatalogSync;

// Yurguen: Sincroniza catálogo proveedor -> BD, precios con margen+IVA+descuento, oculta por días sin stock.
public sealed class CatalogSynchronizer
{
    private readonly MayoreoKennethDbContext _db;
    private readonly IExternalCatalogSource _external;
    private readonly ILogger<CatalogSynchronizer> _logger;
    private static readonly Guid DefaultSupplierId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public CatalogSynchronizer(
        MayoreoKennethDbContext db,
        IExternalCatalogSource external,
        ILogger<CatalogSynchronizer> logger)
    {
        _db = db;
        _external = external;
        _logger = logger;
    }

    public async Task<CatalogSyncLog> RunAsync(CancellationToken cancellationToken)
    {
        var log = new CatalogSyncLog
        {
            Id = Guid.NewGuid(),
            StartedAtUtc = DateTime.UtcNow,
            Success = false
        };

        _db.CatalogSyncLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var cfg = await _db.StoreSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
                      ?? throw new InvalidOperationException("StoreSettings no inicializado.");

            await EnsureDefaultSupplierExistsAsync(cancellationToken);

            var externalItems = await _external.FetchCatalogAsync(cancellationToken);
            var added = 0;
            var updated = 0;
            var hidden = 0;
            var utcNow = DateTime.UtcNow;

            foreach (var item in externalItems)
            {
                var map = await _db.ProductSupplierMaps
                    .Include(x => x.Product)
                    .FirstOrDefaultAsync(
                        x => x.SupplierId == DefaultSupplierId && x.SupplierProductId == item.SupplierProductId,
                        cancellationToken);

                if (map is null)
                {
                    var product = new Product
                    {
                        Id = Guid.NewGuid(),
                        Sku = item.Sku,
                        Name = item.Name,
                        Description = item.Description,
                        ImageUrl = item.ImageUrl,
                        IsPublished = true,
                        IsCatalogHidden = false,
                        IvaIncludedInDisplayPrice = true,
                        AdminDiscountPercent = 0,
                        DisplayPriceWithIva = 0,
                        CreatedAtUtc = utcNow
                    };

                    map = new ProductSupplierMap
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        SupplierId = DefaultSupplierId,
                        SupplierProductId = item.SupplierProductId,
                        SupplierSku = item.Sku,
                        LastKnownCost = item.SupplierCost,
                        LastSyncedAtUtc = utcNow,
                        IsActive = true
                    };

                    _db.Products.Add(product);
                    _db.ProductSupplierMaps.Add(map);
                    added++;
                }
                else
                {
                    map.LastKnownCost = item.SupplierCost;
                    map.LastSyncedAtUtc = utcNow;
                    map.Product.Name = item.Name;
                    map.Product.Description = item.Description;
                    map.Product.ImageUrl = item.ImageUrl;
                    map.Product.Sku = item.Sku;
                    map.Product.UpdatedAtUtc = utcNow;
                    updated++;
                }

                var inStockThisSync = item.StockQuantity > 0;
                ApplyStockVisibility(map.Product, cfg, utcNow, inStockThisSync, ref hidden);
                map.Product.DisplayPriceWithIva = DisplayPriceCalculator.ComputeDisplayPriceWithIva(
                    map.LastKnownCost,
                    cfg.DefaultMarkupPercent,
                    cfg.IvaPercentOnSale,
                    map.Product.AdminDiscountPercent);
            }

            var settingsRow = await _db.StoreSettings.FirstAsync(cancellationToken);
            settingsRow.LastCatalogSyncCompletedAtUtc = utcNow;
            settingsRow.UpdatedAtUtc = utcNow;

            log.CompletedAtUtc = DateTime.UtcNow;
            log.Success = true;
            log.RowsAdded = added;
            log.RowsUpdated = updated;
            log.RowsHidden = hidden;

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Yurguen: CatalogSync OK. Agregados={Added} Actualizados={Updated} Ocultos={Hidden}", added, updated, hidden);
        }
        catch (Exception ex)
        {
            log.CompletedAtUtc = DateTime.UtcNow;
            log.Success = false;
            log.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Yurguen: CatalogSync falló.");
        }

        return log;
    }

    private async Task EnsureDefaultSupplierExistsAsync(CancellationToken cancellationToken)
    {
        if (await _db.Suppliers.AnyAsync(x => x.Id == DefaultSupplierId, cancellationToken))
        {
            return;
        }

        _db.Suppliers.Add(new Supplier
        {
            Id = DefaultSupplierId,
            Name = "Proveedor simulado",
            Code = "SIM",
            ApiBaseUrl = null,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    // Yurguen: Solo usamos si hubo o no disponibilidad en este sync; la cantidad no se persiste en BD.
    private static void ApplyStockVisibility(
        Product product,
        StoreSettings cfg,
        DateTime utcNow,
        bool inStockThisSync,
        ref int hiddenCount)
    {
        if (inStockThisSync)
        {
            product.FirstOutOfStockAtUtc = null;
            if (product.IsCatalogHidden)
            {
                product.IsCatalogHidden = false;
            }

            return;
        }

        product.FirstOutOfStockAtUtc ??= utcNow;
        var days = (utcNow - product.FirstOutOfStockAtUtc.Value).TotalDays;
        if (days >= cfg.UnavailableHideAfterDays && !product.IsCatalogHidden)
        {
            product.IsCatalogHidden = true;
            hiddenCount++;
        }
    }
}
