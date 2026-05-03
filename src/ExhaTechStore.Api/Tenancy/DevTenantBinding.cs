namespace ExhaTechStore.Api.Tenancy;

// Yurguen: Entrada Catalog:DevTenants para resolver slug → Guid sin Postgres (UseInMemory).
public sealed class DevTenantBinding
{
    public string Slug { get; set; } = "";

    public string Id { get; set; } = "";

    /// <summary>Yurguen: Nombre bonito para el panel dueños cuando no hay Postgres.</summary>
    public string? DisplayName { get; set; }
}
