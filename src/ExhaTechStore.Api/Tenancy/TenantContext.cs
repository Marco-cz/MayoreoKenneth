namespace ExhaTechStore.Api.Tenancy;

// Yurguen: Implementación scoped poblada por TenantSlugResolutionFilter.
public sealed class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public bool IsResolved { get; private set; }

    public void SetTenant(Guid tenantId, string slug)
    {
        TenantId = tenantId;
        Slug = slug;
        IsResolved = true;
    }
}
