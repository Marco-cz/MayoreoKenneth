using ExhaTechStore.Domain.Enums;

namespace ExhaTechStore.Domain.Entities;

public class Order
{
    // Yurguen: Orden de compra creada desde la tienda (por tenant).
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAtUtc { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
