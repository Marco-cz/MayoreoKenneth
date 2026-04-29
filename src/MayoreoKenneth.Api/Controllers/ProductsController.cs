using MayoreoKenneth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MayoreoKenneth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly MayoreoKennethDbContext _dbContext;

    public ProductsController(MayoreoKennethDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        // Yurguen: Endpoint inicial para listar productos del catalogo.
        var products = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ProductListItemResponse(
                x.Id,
                x.Sku,
                x.Name,
                x.IsPublished,
                x.UpdatedAtUtc ?? x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(products);
    }
}

public sealed record ProductListItemResponse(
    Guid Id,
    string Sku,
    string Name,
    bool IsPublished,
    DateTime LastUpdatedAtUtc);
