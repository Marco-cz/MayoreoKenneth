namespace ExhaTechStore.Domain.Entities;

// Yurguen: Cliente SaaS (tienda). URL pública: /{Slug} en el front; API: /t/{slug}/api/...
public class Tenant
{
    public Guid Id { get; set; }

    /// <summary>Yurguen: Segmento URL minúsculas, sin espacios (ej. mayoreokenneth, ninadesigns).</summary>
    public string Slug { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    public ICollection<StoreSettings> StoreSettingsRows { get; set; } = new List<StoreSettings>();
}
