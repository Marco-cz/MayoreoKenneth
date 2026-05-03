using ExhaTechStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ExhaTechStore.Api.Tenancy;

// Yurguen: Resuelve slug de ruta a TenantId (BD o modo dev Catalog:DevTenants).
public sealed class TenantSlugResolutionFilter : IAsyncActionFilter
{
    private readonly IConfiguration _configuration;

    public TenantSlugResolutionFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.RouteData.Values.TryGetValue("tenantSlug", out var slugObj)
            || slugObj is not string slugRaw)
        {
            await next();
            return;
        }

        var slugNormalized = slugRaw.Trim().ToLowerInvariant();
        var tenantCtx =
            context.HttpContext.RequestServices.GetRequiredService<TenantContext>();

        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            var devTenants =
                _configuration.GetSection("Catalog:DevTenants").Get<List<DevTenantBinding>>();
            var match = devTenants?.FirstOrDefault(t =>
                string.Equals(t.Slug.Trim(), slugNormalized, StringComparison.OrdinalIgnoreCase));

            if (match is null || string.IsNullOrWhiteSpace(match.Id))
            {
                context.Result =
                    new NotFoundObjectResult(new { message = "Tienda no encontrada." });
                return;
            }

            if (!Guid.TryParse(match.Id, out var tenantId))
            {
                context.Result = new BadRequestObjectResult(new { message = "Slug de tienda mal configurado." });
                return;
            }

            tenantCtx.SetTenant(tenantId, slugNormalized);
            await next();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<ExhaTechStoreDbContext>();
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Slug == slugNormalized,
                context.HttpContext.RequestAborted);

        if (tenant is null || !tenant.IsActive)
        {
            context.Result = new NotFoundObjectResult(new { message = "Tienda no encontrada." });
            return;
        }

        tenantCtx.SetTenant(tenant.Id, tenant.Slug);
        await next();
    }
}
