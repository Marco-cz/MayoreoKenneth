namespace MayoreoKenneth.Infrastructure.Pricing;

// Yurguen: Precio público = costo * (1+margen%) * (1-descuento%) * (1+IVA%). Redondeo comercial.
public static class DisplayPriceCalculator
{
    public static decimal ComputeDisplayPriceWithIva(
        decimal supplierCost,
        decimal markupPercent,
        decimal ivaPercent,
        decimal adminDiscountPercent)
    {
        var discount = Math.Clamp(adminDiscountPercent, 0m, 100m);
        var withMarkup = supplierCost * (1 + markupPercent / 100m);
        var afterDiscount = withMarkup * (1 - discount / 100m);
        if (afterDiscount < 0)
        {
            afterDiscount = 0;
        }

        var withIva = afterDiscount * (1 + ivaPercent / 100m);
        return Math.Round(withIva, 2, MidpointRounding.AwayFromZero);
    }
}
