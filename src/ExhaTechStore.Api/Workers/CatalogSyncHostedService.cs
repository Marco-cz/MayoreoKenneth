using ExhaTechStore.Api.Tenancy;
using ExhaTechStore.Infrastructure.CatalogSync;
using ExhaTechStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExhaTechStore.Api.Workers;

// Yurguen: Job diario: sincroniza catálogo para cada tenant activo (o DevTenants en modo memoria).
public sealed class CatalogSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CatalogSyncHostedService> _logger;

    public CatalogSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<CatalogSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("Jobs:CatalogSyncEnabled", false))
        {
            _logger.LogInformation("Yurguen: CatalogSyncHostedService deshabilitado (Jobs:CatalogSyncEnabled=false).");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = ComputeDelayUntilNextRun();
                _logger.LogInformation("Yurguen: CatalogSync próxima ejecución en {Delay}.", delay);
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<CatalogSynchronizer>();

                foreach (var tenantId in await ListTenantIdsForSyncAsync(scope, stoppingToken))
                {
                    await sync.RunForTenantAsync(tenantId, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yurguen: Error en CatalogSyncHostedService.");
            }
        }
    }

    private async Task<List<Guid>> ListTenantIdsForSyncAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue("Catalog:UseInMemory", true))
        {
            var db = scope.ServiceProvider.GetRequiredService<ExhaTechStoreDbContext>();
            return await db.Tenants.AsNoTracking()
                .Where(t => t.IsActive)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);
        }

        var devTenants = _configuration
            .GetSection("Catalog:DevTenants")
            .Get<List<DevTenantBinding>>() ?? [];

        var ids = new List<Guid>();
        foreach (var d in devTenants)
        {
            if (!string.IsNullOrWhiteSpace(d.Id) && Guid.TryParse(d.Id.Trim(), out var gid))
            {
                ids.Add(gid);
            }
        }

        if (ids.Count == 0)
        {
            _logger.LogWarning("Yurguen: CatalogSync con UseInMemory=true pero Catalog:DevTenants vacío; no hay tenants.");
        }

        return ids;
    }

    private TimeSpan ComputeDelayUntilNextRun()
    {
        var tzId = _configuration["Store:TimeZoneId"] ?? "America/Costa_Rica";
        var hour = _configuration.GetValue("Store:CatalogSyncHourLocal", 2);
        var minute = _configuration.GetValue("Store:CatalogSyncMinuteLocal", 0);

        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
        var targetLocal = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, hour, minute, 0, DateTimeKind.Unspecified);
        if (nowLocal >= targetLocal)
        {
            targetLocal = targetLocal.AddDays(1);
        }

        var targetUtc = TimeZoneInfo.ConvertTimeToUtc(targetLocal, tz);
        var delay = targetUtc - nowUtc;
        if (delay < TimeSpan.FromMinutes(1))
        {
            delay = TimeSpan.FromMinutes(1);
        }

        return delay;
    }
}
