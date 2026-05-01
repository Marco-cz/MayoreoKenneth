namespace MayoreoKenneth.Infrastructure.CatalogSync;

// Yurguen: Abstracción del catálogo proveedor; hoy implementación simulada.
public interface IExternalCatalogSource
{
    Task<IReadOnlyList<ExternalCatalogItemDto>> FetchCatalogAsync(CancellationToken cancellationToken);
}
