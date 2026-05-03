using ExhaTechStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExhaTechStore.Api.Workers;

// Yurguen: Purga de datos operativos viejos (no toca órdenes fiscales).
public sealed class DataRetentionHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataRetentionHostedService> _logger;

    public DataRetentionHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<DataRetentionHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("Jobs:RetentionEnabled", false))
        {
            _logger.LogInformation("Yurguen: DataRetentionHostedService deshabilitado (Jobs:RetentionEnabled=false).");
            return;
        }

        var retentionDays = _configuration.GetValue("Jobs:CatalogSyncLogRetentionDays", 90);

        await PurgeOnceAsync(retentionDays, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await PurgeOnceAsync(retentionDays, stoppingToken);
        }
    }

    private async Task PurgeOnceAsync(int retentionDays, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ExhaTechStoreDbContext>();
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            var oldLogs = await db.CatalogSyncLogs
                .Where(x => x.StartedAtUtc < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            if (oldLogs > 0)
            {
                _logger.LogInformation("Yurguen: Retención eliminó {Count} filas de CatalogSyncLogs (> {Days} días).", oldLogs, retentionDays);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yurguen: Error en DataRetentionHostedService.");
        }
    }
}
