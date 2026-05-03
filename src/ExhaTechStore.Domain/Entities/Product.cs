using ExhaTechStore.Domain.Enums;

namespace ExhaTechStore.Domain.Entities;

public class Product
{
    // Yurguen: Producto publicado en el catálogo de un tenant (tienda SaaS).
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Yurguen: URL de imagen del catálogo proveedor; no almacenamos binarios.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Yurguen: Precio mostrado al público con IVA incluido (calculado en sync).</summary>
    public decimal DisplayPriceWithIva { get; set; }

    /// <summary>Yurguen: Si true, la UI debe indicar "IVA incluido".</summary>
    public bool IvaIncludedInDisplayPrice { get; set; } = true;

    /// <summary>Yurguen: Descuento admin 0–100 % sobre precio con margen (antes de IVA).</summary>
    public decimal AdminDiscountPercent { get; set; }

    public bool IsPublished { get; set; } = false;

    /// <summary>Yurguen: Oculto por inventario inactivo prolongado (borrado lógico catálogo).</summary>
    public bool IsCatalogHidden { get; set; }

    /// <summary>
    /// Yurguen: Inicio de la racha actual “sin disponibilidad” según el último sync (UTC); null si ese sync vio stock.
    /// No guardamos cantidades; solo fechas para ocultar tras N días sin disponibilidad.
    /// </summary>
    public DateTime? FirstOutOfStockAtUtc { get; set; }

    /// <summary>
    /// Yurguen: Proveedor vs propio vs híbrido (catálogo externo + stock manual).
    /// </summary>
    public ProductInventoryMode InventoryMode { get; set; } = ProductInventoryMode.SupplierManaged;

    /// <summary>Yurguen: Unidades disponibles propias; aplica TenantManaged e Hybrid.</summary>
    public int ManualStockQuantity { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ProductSupplierMap> SupplierMappings { get; set; } = new List<ProductSupplierMap>();
    public ICollection<PriceRule> PriceRules { get; set; } = new List<PriceRule>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
