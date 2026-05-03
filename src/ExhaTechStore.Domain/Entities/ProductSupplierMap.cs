namespace ExhaTechStore.Domain.Entities;

public class ProductSupplierMap
{
    // Yurguen: Mapea un producto interno al identificador del proveedor.
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierProductId { get; set; } = string.Empty;
    public string? SupplierSku { get; set; }
    public decimal LastKnownCost { get; set; }

    public DateTime? LastSyncedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public Product Product { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
}
