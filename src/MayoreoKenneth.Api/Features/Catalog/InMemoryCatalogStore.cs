namespace MayoreoKenneth.Api.Features.Catalog;

public sealed class InMemoryCatalogStore
{
    private static readonly IReadOnlyList<CatalogProduct> ProductsSeed =
    [
        new CatalogProduct(Guid.Parse("11111111-1111-1111-1111-111111111111"), "MK-ARROZ-001", "Arroz Premium 5kg", "Arroz de grano largo para pulperia y minisuper.", 22.50m, true, 25),
        new CatalogProduct(Guid.Parse("22222222-2222-2222-2222-222222222222"), "MK-AZUCAR-001", "Azucar Blanca 2kg", "Presentacion al por mayor con precio competitivo.", 9.75m, true, 8),
        new CatalogProduct(Guid.Parse("33333333-3333-3333-3333-333333333333"), "MK-FRIJOL-001", "Frijol Negro 1kg", "Producto de alta rotacion para comercios.", 7.90m, true, 12),
        new CatalogProduct(Guid.Parse("44444444-4444-4444-4444-444444444444"), "MK-ACEITE-001", "Aceite Vegetal 900ml", "Empaque resistente para distribucion local.", 6.25m, true, 6)
    ];

    public IReadOnlyList<CatalogProduct> GetProducts()
    {
        return ProductsSeed.OrderBy(x => x.Name).ToList();
    }

    public CatalogProduct? GetById(Guid id)
    {
        return ProductsSeed.FirstOrDefault(x => x.Id == id);
    }
}

public sealed record CatalogProduct(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    decimal Price,
    bool IsPublished,
    int SimulatedStock);
