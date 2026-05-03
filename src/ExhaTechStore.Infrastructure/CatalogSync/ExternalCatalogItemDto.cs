namespace ExhaTechStore.Infrastructure.CatalogSync;

// Yurguen: DTO que simula respuesta del proveedor hasta tener API real.
// StockQuantity: Yurguen: en sync solo usamos >0 como “había disponibilidad”; no persistimos cantidad en nuestra BD.
public sealed record ExternalCatalogItemDto(
    string SupplierProductId,
    string Sku,
    string Name,
    string? Description,
    string? ImageUrl,
    decimal SupplierCost,
    int StockQuantity);
