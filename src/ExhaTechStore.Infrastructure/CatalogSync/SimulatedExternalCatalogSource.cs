namespace ExhaTechStore.Infrastructure.CatalogSync;

// Yurguen: Catálogo simulado + stock en vivo; mismo feed por tenant hasta conectar APIs reales.
public sealed class SimulatedExternalCatalogSource : IExternalCatalogSource, IExternalLiveStockSource
{
    private static readonly ExternalCatalogItemDto[] Items =
    [
        new("EXT-001", "MK-ARROZ-001", "Arroz Premium 5kg", "Grano largo.", null, 18.00m, 40),
        new("EXT-002", "MK-AZUCAR-001", "Azúcar blanca 2kg", null, null, 7.50m, 15),
        new("EXT-003", "MK-FRIJOL-001", "Frijol negro 1kg", null, null, 6.20m, 0),
        new("EXT-004", "MK-ACEITE-001", "Aceite vegetal 900ml", null, null, 4.90m, 22)
    ];

    public Task<IReadOnlyList<ExternalCatalogItemDto>> FetchCatalogAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        return Task.FromResult<IReadOnlyList<ExternalCatalogItemDto>>(Items);
    }

    public Task<ExternalLiveStockOutcome> QueryLiveStockAsync(
        Guid tenantId,
        ExternalLiveStockQuery query,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        var item = FindItem(query);
        var utc = DateTime.UtcNow;
        if (item is null)
        {
            return Task.FromResult(new ExternalLiveStockOutcome(true, false, null, utc, null));
        }

        return Task.FromResult(new ExternalLiveStockOutcome(true, true, item.StockQuantity, utc, null));
    }

    private static ExternalCatalogItemDto? FindItem(ExternalLiveStockQuery q)
    {
        if (!string.IsNullOrWhiteSpace(q.SupplierProductId))
        {
            var byPid = Items.FirstOrDefault(x =>
                string.Equals(x.SupplierProductId, q.SupplierProductId, StringComparison.OrdinalIgnoreCase));
            if (byPid is not null)
            {
                return byPid;
            }
        }

        if (!string.IsNullOrWhiteSpace(q.SupplierSku))
        {
            return Items.FirstOrDefault(x =>
                string.Equals(x.Sku, q.SupplierSku, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }
}
