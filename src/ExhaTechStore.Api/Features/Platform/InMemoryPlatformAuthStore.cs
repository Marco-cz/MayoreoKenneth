using ExhaTechStore.Api.Features.Auth;

namespace ExhaTechStore.Api.Features.Platform;

// Yurguen: Credenciales de dueños de la plataforma (no son usuarios de una tienda tenant).
public sealed class InMemoryPlatformAuthStore
{
    private readonly IConfiguration _configuration;

    public InMemoryPlatformAuthStore(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AuthUser? ValidateOwner(string email, string password)
    {
        var expectedEmail = (_configuration["Platform:OwnerEmail"] ?? "dueño@exhatech.store").Trim();
        var expectedPass = _configuration["Platform:OwnerPassword"] ?? "Plataforma123*";
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return string.Equals(email.Trim(), expectedEmail, StringComparison.OrdinalIgnoreCase)
               && password == expectedPass
            ? new AuthUser(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                expectedEmail,
                "",
                _configuration["Platform:OwnerNombre"] ?? "Dueño ExhaTech",
                "PlatformOwner")
            : null;
    }
}
