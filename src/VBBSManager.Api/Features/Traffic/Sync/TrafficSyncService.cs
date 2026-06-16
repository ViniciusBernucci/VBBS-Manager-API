using VBBSManager.Api.Common.Results;
using VBBSManager.Infrastructure.ExternalClients.Meta;

namespace VBBSManager.Api.Features.Traffic.Sync;

public interface ITrafficSyncService
{
    Task<Result<TrafficSyncResponse>> ExecuteAsync(
        Guid tenantId, int year, int month, CancellationToken ct);
}

public class TrafficSyncService(
    IMetaAdsMonthSyncService syncService,
    ILogger<TrafficSyncService> logger) : ITrafficSyncService
{
    public async Task<Result<TrafficSyncResponse>> ExecuteAsync(
        Guid tenantId, int year, int month, CancellationToken ct)
    {
        if (year < 2020 || year > DateTime.UtcNow.Year + 1)
            return Result<TrafficSyncResponse>.Fail("Ano inválido.");

        if (month < 1 || month > 12)
            return Result<TrafficSyncResponse>.Fail("Mês inválido (1–12).");

        logger.LogInformation(
            "Traffic sync solicitado — tenant {TenantId}, {Year}/{Month}",
            tenantId, year, month);

        MetaSyncSummary summary;
        try
        {
            summary = await syncService.SyncMonthAsync(tenantId, year, month, ct);
        }
        catch (MetaTokenException)
        {
            return Result<TrafficSyncResponse>.Fail("Token Meta inválido ou expirado. Gere um novo System User Token com permissão ads_read.");
        }
        catch (MetaPermissionException)
        {
            return Result<TrafficSyncResponse>.Fail("Token Meta sem permissão ads_read. Adicione essa permissão ao token no Meta Business Manager.");
        }
        catch (MetaRateLimitException)
        {
            return Result<TrafficSyncResponse>.Fail("Rate limit da API Meta atingido. Aguarde alguns minutos e tente novamente.");
        }
        catch (MetaApiException ex)
        {
            return Result<TrafficSyncResponse>.Fail($"Erro na API Meta (código {ex.Code}): {ex.Message}");
        }

        return Result<TrafficSyncResponse>.Ok(new TrafficSyncResponse(
            Campaigns: summary.Campaigns,
            AdSets:    summary.AdSets,
            Ads:       summary.Ads,
            Since:     summary.Since.ToString("yyyy-MM-dd"),
            Until:     summary.Until.ToString("yyyy-MM-dd"),
            Message:   $"Sincronizado: {summary.Campaigns} campanhas · {summary.AdSets} conjuntos · {summary.Ads} anúncios"
        ));
    }
}
