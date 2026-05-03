namespace ExhaTechStore.Api.Features.Auth;

// Yurguen: Solo roles internos (MVP). Compradores no tienen cuenta; Admin = precios/config; Soporte = casos.
public sealed class InMemoryAuthStore
{
    private static readonly IReadOnlyList<AuthUser> Users =
    [
        new AuthUser(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "admin@exhatech.store", "Admin123*", "Administrador demo", "Admin"),
        new AuthUser(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "soporte@exhatech.store", "Soporte123*", "Equipo Soporte demo", "Soporte")
    ];

    public AuthUser? ValidateCredentials(string email, string password)
    {
        return Users.FirstOrDefault(x =>
            string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase) &&
            x.Password == password);
    }
}

public sealed record AuthUser(
    Guid Id,
    string Email,
    string Password,
    string FullName,
    string Role);
