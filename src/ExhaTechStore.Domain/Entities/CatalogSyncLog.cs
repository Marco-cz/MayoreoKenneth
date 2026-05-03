namespace ExhaTechStore.Domain.Entities;

// Yurguen: Historial de sincronizaciones por tenant. Retención operativa: 90 días (job DataRetentionHostedService).
public class CatalogSyncLog
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool Success { get; set; }
    public int RowsAdded { get; set; }
    public int RowsUpdated { get; set; }
    public int RowsHidden { get; set; }
    public string? ErrorMessage { get; set; }
}
