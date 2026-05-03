namespace ExhaTechStore.Infrastructure.CatalogSync;

// Yurguen: Consulta puntual de disponibilidad al proveedor (no reemplaza el sync de catálogo).
public interface IExternalLiveStockSource
{
    /// <summary>
    /// Yurguen: Devuelve stock actual del proveedor para un SKU o id de producto proveedor.
    /// </summary>
    Task<ExternalLiveStockOutcome> QueryLiveStockAsync(
        Guid tenantId,
        ExternalLiveStockQuery query,
        CancellationToken cancellationToken);
}
