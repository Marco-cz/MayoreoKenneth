namespace ExhaTechStore.Api.Features.Soporte;

// Yurguen: Casos de soporte en memoria (MVP); resolucion = borrado logico (ResueltoEnUtc).
public sealed class InMemorySupportTicketsStore
{
    private readonly List<CasoSoporte> _casos = [];
    private readonly object _sync = new();

    public CasoSoporte Agregar(string correo, string nombreContacto, string? numeroOrden, string asunto, string descripcion)
    {
        var caso = new CasoSoporte(
            Guid.NewGuid(),
            correo.Trim(),
            nombreContacto.Trim(),
            string.IsNullOrWhiteSpace(numeroOrden) ? null : numeroOrden.Trim(),
            asunto.Trim(),
            descripcion.Trim(),
            DateTime.UtcNow,
            ResueltoEnUtc: null);

        lock (_sync)
        {
            _casos.Insert(0, caso);
        }

        return caso;
    }

    // Yurguen: Bandeja admin solo muestra casos activos (no resueltos).
    public IReadOnlyList<CasoSoporte> ListarActivos()
    {
        lock (_sync)
        {
            return _casos
                .Where(x => x.ResueltoEnUtc is null)
                .OrderByDescending(x => x.CreadoEnUtc)
                .ToList();
        }
    }

    // Yurguen: Borrado logico: marca fecha de resolucion; el registro sigue en memoria para auditoria MVP.
    public bool MarcarResuelto(Guid id)
    {
        lock (_sync)
        {
            var idx = _casos.FindIndex(x => x.Id == id && x.ResueltoEnUtc is null);
            if (idx < 0)
            {
                return false;
            }

            _casos[idx] = _casos[idx] with { ResueltoEnUtc = DateTime.UtcNow };
            return true;
        }
    }
}

public sealed record CasoSoporte(
    Guid Id,
    string Correo,
    string NombreContacto,
    string? NumeroOrden,
    string Asunto,
    string Descripcion,
    DateTime CreadoEnUtc,
    DateTime? ResueltoEnUtc);
