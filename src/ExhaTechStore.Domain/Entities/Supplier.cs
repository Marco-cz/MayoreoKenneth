namespace ExhaTechStore.Domain.Entities;

public class Supplier
{
    // Yurguen: Proveedor de catálogo/inventario externo, por tenant.
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ApiBaseUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ProductSupplierMap> ProductMappings { get; set; } = new List<ProductSupplierMap>();
}
