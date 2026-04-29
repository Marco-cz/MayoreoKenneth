namespace MayoreoKenneth.Domain.Entities;

public class Supplier
{
    // Yurguen: Representa un proveedor externo de inventario.
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ApiBaseUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ProductSupplierMap> ProductMappings { get; set; } = new List<ProductSupplierMap>();
}
