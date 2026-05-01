namespace MayoreoKenneth.Api.Features.Checkout;

public sealed class InMemoryCheckoutStore
{
    private int _sequence = 1000;
    private readonly List<CheckoutSimulationResult> _orders = [];
    private readonly object _sync = new();

    public CheckoutSimulationResult SimulateCheckout(
        IReadOnlyList<CheckoutItemRequest> items,
        DatosFacturacionMvp datosFacturacion,
        DireccionEntregaMvp direccionEntrega)
    {
        var subtotal = items.Sum(x => x.UnitPrice * x.Quantity);
        var orderNumber = $"MK-MVP-{Interlocked.Increment(ref _sequence)}";
        var result = new CheckoutSimulationResult(
            Guid.NewGuid(),
            orderNumber,
            subtotal,
            "SIMULATED",
            "PENDING_MVP",
            DateTime.UtcNow,
            datosFacturacion,
            direccionEntrega);

        lock (_sync)
        {
            // Yurguen: Historial temporal MVP para visualizar ordenes sin BD.
            _orders.Insert(0, result);
        }

        // Yurguen: Este resultado es temporal MVP; luego se reemplaza por orden real + pago real.
        return result;
    }

    public IReadOnlyList<CheckoutSimulationResult> GetOrders()
    {
        lock (_sync)
        {
            return _orders.ToList();
        }
    }

    public CheckoutSimulationResult? ConfirmOrder(Guid orderId)
    {
        lock (_sync)
        {
            var order = _orders.FirstOrDefault(x => x.OrderId == orderId);
            if (order is null)
            {
                return null;
            }

            if (order.OrderStatus is "CANCELLED_MVP" or "CONFIRMED_MVP")
            {
                return order;
            }

            var updated = order with { OrderStatus = "CONFIRMED_MVP" };
            _orders[_orders.FindIndex(x => x.OrderId == orderId)] = updated;
            return updated;
        }
    }

    public CheckoutSimulationResult? CancelOrder(Guid orderId)
    {
        lock (_sync)
        {
            var order = _orders.FirstOrDefault(x => x.OrderId == orderId);
            if (order is null)
            {
                return null;
            }

            if (order.OrderStatus is "CONFIRMED_MVP" or "CANCELLED_MVP")
            {
                return order;
            }

            var updated = order with { OrderStatus = "CANCELLED_MVP" };
            _orders[_orders.FindIndex(x => x.OrderId == orderId)] = updated;
            return updated;
        }
    }
}

public sealed record CheckoutItemRequest(Guid ProductId, int Quantity, decimal UnitPrice);

public sealed record CheckoutSimulationResult(
    Guid OrderId,
    string OrderNumber,
    decimal TotalAmount,
    string PaymentStatus,
    string OrderStatus,
    DateTime CreatedAtUtc,
    DatosFacturacionMvp DatosFacturacion,
    DireccionEntregaMvp DireccionEntrega);
