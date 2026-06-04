using Hangfire;
using Microsoft.Extensions.Logging;

namespace VBBSManager.Infrastructure.Jobs;

public class HotmartSyncJob(ILogger<HotmartSyncJob> logger)
{
    public const string JobId = "hotmart-sync";

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900])]
    public async Task ExecuteAsync(Guid tenantId, CancellationToken ct = default)
    {
        logger.LogInformation("Starting Hotmart sync for tenant {TenantId}", tenantId);

        // TODO: buscar credenciais Hotmart do tenant no banco
        // TODO: chamar HotmartClient para obter vendas do dia anterior
        // TODO: persistir vendas com tenant_id

        await Task.CompletedTask;

        logger.LogInformation("Hotmart sync completed for tenant {TenantId}", tenantId);
    }
}
