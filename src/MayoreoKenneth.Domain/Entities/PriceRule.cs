namespace MayoreoKenneth.Domain.Entities;

public class PriceRule
{
    // Yurguen: Regla de margen para calcular precio de venta.
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal MarkupPercentage { get; set; }
    public decimal? MinimumMarginAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndsAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
}
