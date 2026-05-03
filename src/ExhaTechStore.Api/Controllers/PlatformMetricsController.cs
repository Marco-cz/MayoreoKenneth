using ExhaTechStore.Api.Features.Checkout;
using ExhaTechStore.Api.Tenancy;
using ExhaTechStore.Domain.Enums;
using ExhaTechStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExhaTechStore.Api.Controllers;

// Yurguen: Resumen para dashboard dueños (gráficos MVP; comisión ejemplar hasta traza pasarela real).
[ApiController]
[Route("api/platform/[controller]")]
[Authorize(Roles = "PlatformOwner")]
public class PlatformMetricsController : ControllerBase
{
    private readonly InMemoryCheckoutStore _mvpCheckout;
    private readonly ExhaTechStoreDbContext _db;
    private readonly IConfiguration _configuration;

    public PlatformMetricsController(
        InMemoryCheckoutStore mvpCheckout,
        ExhaTechStoreDbContext db,
        IConfiguration configuration)
    {
        _mvpCheckout = mvpCheckout;
        _db = db;
        _configuration = configuration;
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken ct)
    {
        var pct = _configuration.GetValue("Platform:ComisionEjemploPct", 2.9m);
        if (_configuration.GetValue("Catalog:UseInMemory", true))
        {
            var puntos = VentasMvpUltimosMeses(_mvpCheckout, meses: 6);
            var dto = puntos.Select(p =>
                new VentaMesDto(p.Mes, p.Monto, p.Ordenes, decimal.Round(p.Monto * pct / 100m, 2))).ToList();
            var tenants =
                (_configuration.GetSection("Catalog:DevTenants").Get<List<DevTenantBinding>>() ?? []).Count;
            return Ok(new MetricasPlataformaDto(
                "inMemory",
                dto,
                new TotalesDashboardDto(MvpOrdersCount(_mvpCheckout), tenants <= 0 ? 0 : tenants, pct)));
        }

        var desde = DateTime.UtcNow.Date.AddMonths(-11).AddDays(1 - DateTime.UtcNow.Day);
        var ordenesBd = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAtUtc >= desde && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => new { o.CreatedAtUtc.Year, o.CreatedAtUtc.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Monto = g.Sum(x => x.TotalAmount), C = g.Count() })
            .ToListAsync(ct);

        var map = ordenesBd.ToDictionary(x => $"{x.Year:D4}-{x.Month:D2}", x => (x.Monto, x.C));
        var serie = GetUltimosMeses(desde, 12)
            .Select(m =>
            {
                map.TryGetValue(m, out var v);
                var monto = v.Monto;
                return new VentaMesDto(m, monto, v.C, decimal.Round(monto * pct / 100m, 2));
            })
            .ToList();

        var porTenant = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAtUtc >= desde && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.TenantId)
            .Select(g => new { Tid = g.Key, Monto = g.Sum(x => x.TotalAmount) })
            .ToListAsync(ct);

        var idsTenant = porTenant.Select(p => p.Tid).Distinct().ToList();
        var slugMap = await _db.Tenants.AsNoTracking()
            .Where(t => idsTenant.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Slug, ct);

        decimal totalVt = porTenant.Sum(x => x.Monto);
        var donut = porTenant.Select(p => new TenantShareDto(
            slugMap.TryGetValue(p.Tid, out var s) ? s : p.Tid.ToString(),
            p.Monto,
            totalVt > 0 ? decimal.Round(100m * p.Monto / totalVt, 1) : 0)).ToList();

        var countTenants = await _db.Tenants.CountAsync(t => t.IsActive, ct);
        var ordenesTot = await _db.Orders.CountAsync(
            o => o.CreatedAtUtc >= desde && o.Status != OrderStatus.Cancelled,
            ct);

        return Ok(new MetricasPlataformaDto(
            "database",
            serie,
            new TotalesDashboardDto(ordenesTot, countTenants, pct),
            donut));
    }

    private static IEnumerable<string> GetUltimosMeses(DateTime desde, int meses)
    {
        var d = new DateTime(desde.Year, desde.Month, 1);
        var fin = DateTime.UtcNow.Date;
        var hasta = new DateTime(fin.Year, fin.Month, 1).AddMonths(1);
        while (d < hasta && meses-- > 0)
        {
            yield return $"{d.Year:D4}-{d.Month:D2}";
            d = d.AddMonths(1);
        }
    }

    private static int MvpOrdersCount(InMemoryCheckoutStore store)
        => store.GetOrders().Count(o => o.OrderStatus != "CANCELLED_MVP");

    private static IReadOnlyList<(string Mes, decimal Monto, int Ordenes)> VentasMvpUltimosMeses(
        InMemoryCheckoutStore store,
        int meses)
    {
        var datos = store.GetOrders().Where(o => o.OrderStatus != "CANCELLED_MVP").ToList();
        var porMes = datos
            .GroupBy(o => $"{o.CreatedAtUtc.Year:D4}-{o.CreatedAtUtc.Month:D2}")
            .ToDictionary(
                g => g.Key,
                g => (Monto: g.Sum(x => x.TotalAmount), C: g.Count()));

        var ahora = DateTime.UtcNow;
        var lista = new List<(string Mes, decimal Monto, int Ordenes)>();
        for (var i = meses - 1; i >= 0; i--)
        {
            var d = new DateTime(ahora.Year, ahora.Month, 1).AddMonths(-i);
            var key = $"{d.Year:D4}-{d.Month:D2}";
            porMes.TryGetValue(key, out var v);
            lista.Add((key, v.Monto, v.C));
        }

        return lista;
    }

    public sealed record MetricasPlataformaDto(
        string Modo,
        IReadOnlyList<VentaMesDto> VentasPorMes,
        TotalesDashboardDto Totales,
        IReadOnlyList<TenantShareDto>? PorTenantSlug = null);

    public sealed record VentaMesDto(string Mes, decimal MontoVentas, int Ordenes, decimal ComisionEstimada);

    public sealed record TotalesDashboardDto(int OrdenesPeriodo, int TenantsActivos, decimal ComisionEjemploPct);

    public sealed record TenantShareDto(string Slug, decimal MontoVentas, decimal PctParticipacion);
}
