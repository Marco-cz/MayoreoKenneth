using MayoreoKenneth.Api.Features.Checkout;
using Microsoft.AspNetCore.Mvc;

namespace MayoreoKenneth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly InMemoryCheckoutStore _checkoutStore;

    public OrdersController(InMemoryCheckoutStore checkoutStore)
    {
        _checkoutStore = checkoutStore;
    }

    [HttpGet("mvp-history")]
    public IActionResult GetMvpHistory()
    {
        // Yurguen: Endpoint temporal MVP para listar ordenes simuladas sin base de datos.
        var orders = _checkoutStore.GetOrders();
        return Ok(orders);
    }

    [HttpPost("{orderId:guid}/confirm-mvp")]
    public IActionResult ConfirmMvp(Guid orderId)
    {
        // Yurguen: Accion temporal MVP para simular confirmacion de orden.
        var updated = _checkoutStore.ConfirmOrder(orderId);
        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpPost("{orderId:guid}/cancel-mvp")]
    public IActionResult CancelMvp(Guid orderId)
    {
        // Yurguen: Accion temporal MVP para simular cancelacion de orden.
        var updated = _checkoutStore.CancelOrder(orderId);
        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }
}
