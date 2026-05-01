namespace MayoreoKenneth.Infrastructure.CatalogSync;

// Yurguen: Identificadores que el conector real mapeará a la API del proveedor.
public sealed record ExternalLiveStockQuery(string? SupplierProductId, string? SupplierSku);

// Yurguen: Resultado de una consulta en vivo (éxito, no encontrado, o fallo técnico).
public sealed record ExternalLiveStockOutcome(
    bool SupplierResponded,
    bool ProductFound,
    int? Quantity,
    DateTime CheckedAtUtc,
    string? ErrorMessage);
