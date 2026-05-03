namespace ExhaTechStore.Api.Tenancy;

// Yurguen: Contexto del tenant actual (resuelto desde ruta /t/{slug}/...).
public interface ITenantContext
{
    Guid TenantId { get; }

    /// <summary>Slug URL normalizado (minúsculas).</summary>
    string Slug { get; }

    bool IsResolved { get; }
}
