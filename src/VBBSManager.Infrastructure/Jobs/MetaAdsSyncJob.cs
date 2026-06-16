using Hangfire;
using Microsoft.Extensions.Logging;
using VBBSManager.Infrastructure.ExternalClients.Meta;

namespace VBBSManager.Infrastructure.Jobs;

public class MetaAdsSyncJob(
    IMetaAdsMonthSyncService syncService,
    ILogger<MetaAdsSyncJob> logger)
{
    public const string JobId = "meta-ads-sync";

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [300, 900, 1800])]
    public async Task ExecuteAsync(Guid tenantId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        logger.LogInformation(
            "Meta Ads sync job iniciado — tenant {TenantId}, mês {Year}/{Month}",
            tenantId, now.Year, now.Month);

        try
        {
            var summary = await syncService.SyncMonthAsync(tenantId, now.Year, now.Month, ct);

            logger.LogInformation(
                "Meta Ads sync job concluído — {Campaigns} campanhas, {AdSets} conjuntos, {Ads} anúncios",
                summary.Campaigns, summary.AdSets, summary.Ads);
        }
        catch (MetaTokenException ex)
        {
            logger.LogError(ex,
                "Meta Ads: token inválido — tenant {TenantId}. Gere um novo System User Token.", tenantId);
            throw;
        }
        catch (MetaPermissionException ex)
        {
            logger.LogError(ex,
                "Meta Ads: permissão insuficiente — tenant {TenantId}. Verifique escopo ads_read.", tenantId);
            throw;
        }
        catch (MetaRateLimitException ex)
        {
            logger.LogWarning(ex,
                "Meta Ads: rate limit — tenant {TenantId}. Hangfire fará retry.", tenantId);
            throw;
        }
    }
}
