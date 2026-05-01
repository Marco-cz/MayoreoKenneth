using MayoreoKenneth.Api.Features.Checkout;
using MayoreoKenneth.Api.Features.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace MayoreoKenneth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly InMemoryCheckoutStore _checkoutStore;
    private readonly InMemoryCatalogStore _catalogStore;
    private readonly IConfiguration _configuration;

    public CheckoutController(
        InMemoryCheckoutStore checkoutStore,
        InMemoryCatalogStore catalogStore,
        IConfiguration configuration)
    {
        _checkoutStore = checkoutStore;
        _catalogStore = catalogStore;
        _configuration = configuration;
    }

    [HttpPost("simulate")]
    public IActionResult Simulate([FromBody] SimulateCheckoutRequest request)
    {
        if (!_configuration.GetValue("MvpMode:EnableSimulatedCheckout", true))
        {
            return BadRequest(new { message = "Checkout simulado deshabilitado." });
        }

        if (request.Items.Count == 0)
        {
            return BadRequest(new { message = "Debe enviar al menos un producto." });
        }

        var invalidItem = request.Items.FirstOrDefault(x => x.Quantity <= 0 || x.UnitPrice < 0);
        if (invalidItem is not null)
        {
            return BadRequest(new { message = "Cantidad y precio deben ser validos." });
        }

        var datosError = ValidarDatosFacturacion(request.DatosFacturacion);
        if (datosError is not null)
        {
            return BadRequest(new { message = datosError });
        }

        var direccionError = ValidarDireccionEntrega(request.DireccionEntrega);
        if (direccionError is not null)
        {
            return BadRequest(new { message = direccionError });
        }

        // Yurguen: Validacion temporal de stock para simular inventario de tercero.
        var stockValidation = ValidateSimulatedStock(request.Items);
        if (!stockValidation.IsValid)
        {
            return BadRequest(new
            {
                code = "SIMULATED_STOCK_NOT_AVAILABLE",
                message = stockValidation.Message
            });
        }

        // Yurguen: Aqui luego se reemplaza por checkout productivo (stock tercero, pago, persistencia).
        var result = _checkoutStore.SimulateCheckout(
            request.Items
                .Select(x => new CheckoutItemRequest(x.ProductId, x.Quantity, x.UnitPrice))
                .ToList(),
            request.DatosFacturacion,
            request.DireccionEntrega);

        return Ok(result);
    }

    // Yurguen: Validaciones MVP en español; en produccion se amplian con reglas de Hacienda.
    private static string? ValidarDatosFacturacion(DatosFacturacionMvp? d)
    {
        if (d is null)
        {
            return "Los datos de facturacion son obligatorios.";
        }

        if (string.IsNullOrWhiteSpace(d.TipoIdentificacion))
        {
            return "Debe indicar el tipo de identificacion para facturacion.";
        }

        if (string.IsNullOrWhiteSpace(d.NumeroIdentificacion) || d.NumeroIdentificacion.Trim().Length < 5)
        {
            return "El numero de identificacion para facturacion no es valido.";
        }

        if (string.IsNullOrWhiteSpace(d.NombreCompleto))
        {
            return "El nombre completo o razon social para facturacion es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(d.CorreoElectronico) || !d.CorreoElectronico.Contains('@', StringComparison.Ordinal))
        {
            return "El correo electronico para facturacion no es valido.";
        }

        if (string.IsNullOrWhiteSpace(d.Telefono) || d.Telefono.Trim().Length < 8)
        {
            return "El telefono para facturacion debe tener al menos 8 digitos.";
        }

        return null;
    }

    // Yurguen: Direccion de entrega separada de datos fiscales del receptor.
    private static string? ValidarDireccionEntrega(DireccionEntregaMvp? d)
    {
        if (d is null)
        {
            return "La direccion de entrega es obligatoria.";
        }

        if (string.IsNullOrWhiteSpace(d.NombreContacto))
        {
            return "El nombre de contacto para entrega es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(d.Telefono) || d.Telefono.Trim().Length < 8)
        {
            return "El telefono de entrega debe tener al menos 8 digitos.";
        }

        if (string.IsNullOrWhiteSpace(d.Provincia))
        {
            return "La provincia de entrega es obligatoria.";
        }

        if (string.IsNullOrWhiteSpace(d.Canton))
        {
            return "El canton de entrega es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(d.DireccionExacta) || d.DireccionExacta.Trim().Length < 10)
        {
            return "La direccion exacta de entrega debe ser mas detallada (minimo 10 caracteres).";
        }

        return null;
    }

    private (bool IsValid, string Message) ValidateSimulatedStock(IReadOnlyList<SimulateCheckoutItem> items)
    {
        if (!_configuration.GetValue("MvpMode:EnableSimulatedStockValidation", true))
        {
            return (true, string.Empty);
        }

        foreach (var item in items)
        {
            var product = _catalogStore.GetById(item.ProductId);
            if (product is null)
            {
                return (false, "Uno de los productos no existe en el catalogo.");
            }

            if (item.Quantity > product.SimulatedStock)
            {
                return (false, $"Stock insuficiente para {product.Name}. Disponible: {product.SimulatedStock}.");
            }
        }

        return (true, string.Empty);
    }
}

public sealed record SimulateCheckoutRequest(
    IReadOnlyList<SimulateCheckoutItem> Items,
    DatosFacturacionMvp DatosFacturacion,
    DireccionEntregaMvp DireccionEntrega);

public sealed record SimulateCheckoutItem(Guid ProductId, int Quantity, decimal UnitPrice);
