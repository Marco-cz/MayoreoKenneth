namespace MayoreoKenneth.Domain.Entities;

public class Product
{
    // Yurguen: Producto publicado en el catalogo de MayoreoKenneth.
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ProductSupplierMap> SupplierMappings { get; set; } = new List<ProductSupplierMap>();
    public ICollection<PriceRule> PriceRules { get; set; } = new List<PriceRule>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
