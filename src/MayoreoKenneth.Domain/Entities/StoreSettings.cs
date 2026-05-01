namespace MayoreoKenneth.Domain.Entities;

// Yurguen: Configuración global de tienda (una fila). Retención: fila viva; no borrar salvo migración.
public class StoreSettings
{
    public Guid Id { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>Yurguen: Margen por defecto sobre costo proveedor (%). Admin puede cambiarlo.</summary>
    public decimal DefaultMarkupPercent { get; set; } = 40m;

    /// <summary>Yurguen: IVA aplicado al precio sin IVA para obtener precio público con IVA incluido (CR típico 13%).</summary>
    public decimal IvaPercentOnSale { get; set; } = 13m;

    /// <summary>Yurguen: Días con stock 0 antes de ocultar catálogo (borrado lógico). Recomendado 14–30; default 21.</summary>
    public int UnavailableHideAfterDays { get; set; } = 21;

    /// <summary>Yurguen: Hora local CR para ejecutar sync de catálogo (0–23).</summary>
    public int CatalogSyncHourLocal { get; set; } = 2;

    public string TimeZoneId { get; set; } = "America/Costa_Rica";

    public DateTime? LastCatalogSyncCompletedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
