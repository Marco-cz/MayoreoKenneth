namespace ExhaTechStore.Infrastructure.CatalogSync;

// Yurguen: Abstracción del catálogo proveedor por tenant; hoy implementación simulada.
public interface IExternalCatalogSource
{
    Task<IReadOnlyList<ExternalCatalogItemDto>> FetchCatalogAsync(Guid tenantId, CancellationToken cancellationToken);
}
