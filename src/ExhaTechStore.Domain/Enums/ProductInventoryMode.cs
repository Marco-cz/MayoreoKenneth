namespace ExhaTechStore.Domain.Enums;

// Yurguen: Origen de inventario por producto (SaaS multi-tienda). Mezcla = catálogo/costo proveedor + stock propio en BD.
public enum ProductInventoryMode
{
    /// <summary>Yurguen: Disponibilidad según proveedor (sync + consulta en vivo).</summary>
    SupplierManaged = 0,

    /// <summary>Yurguen: Solo stock manual en nuestra BD; no participa del catálogo proveedor.</summary>
    TenantManaged = 1,

    /// <summary>Yurguen: Precio/datos desde proveedor en sync; cantidad vendible = ManualStockQuantity.</summary>
    Hybrid = 2
}
