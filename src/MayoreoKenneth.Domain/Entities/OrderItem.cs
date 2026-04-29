namespace MayoreoKenneth.Domain.Entities;

public class OrderItem
{
    // Yurguen: Linea de producto dentro de una orden.
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid SupplierId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Subtotal { get; set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
}
