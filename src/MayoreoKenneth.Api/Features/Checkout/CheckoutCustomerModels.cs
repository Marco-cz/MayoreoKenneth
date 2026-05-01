namespace MayoreoKenneth.Api.Features.Checkout;

// Yurguen: Datos minimos para factura electronica CR (MVP); luego se amplia con actividad economica, etc.
public sealed record DatosFacturacionMvp(
    string TipoIdentificacion,
    string NumeroIdentificacion,
    string NombreCompleto,
    string CorreoElectronico,
    string Telefono);

// Yurguen: Direccion de entrega separada de facturacion (pueden ser distintas).
public sealed record DireccionEntregaMvp(
    string NombreContacto,
    string Telefono,
    string Provincia,
    string Canton,
    string? Distrito,
    string DireccionExacta,
    string? Referencias);
