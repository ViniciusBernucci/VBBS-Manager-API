using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VBBSManager.Domain.Entities;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Infrastructure.ExternalClients.Meta;

public record MetaSyncSummary(int Campaigns, int AdSets, int Ads, DateOnly Since, DateOnly Until);

public interface IMetaAdsMonthSyncService
{
    Task<MetaSyncSummary> SyncMonthAsync(Guid tenantId, int year, int month, CancellationToken ct = default);
}

public class MetaAdsMonthSyncService(
    IMetaAdsClient client,
    AppDbContext db,
    ILogger<MetaAdsMonthSyncService> logger) : IMetaAdsMonthSyncService
{
    // omni_purchase = métrica canônica do Meta para "Resultados" em campanhas de compra.
    // Cobre offsite (pixel) + onsite (loja) sem duplicar — purchase e offsite_conversion.fb_pixel_purchase
    // reportam os mesmos eventos, o que causaria triple-counting.
    private const string PurchaseActionType = "omni_purchase";

    public async Task<MetaSyncSummary> SyncMonthAsync(
        Guid tenantId, int year, int month, CancellationToken ct = default)
    {
        var since = new DateOnly(year, month, 1);
        var until = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        logger.LogInformation(
            "Meta Ads sync iniciado — tenant {TenantId}, {Since} a {Until}",
            tenantId, since, until);

        // Busca os 3 níveis em paralelo na API
        var (campaignData, adSetData, adData) = await FetchAllLevelsAsync(since, until, ct);

        // Persiste de forma atômica: apaga o mês e reinsere tudo
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await db.MetaCampaignDailyInsights
            .Where(x => x.TenantId == tenantId
                && x.Date >= since && x.Date <= until)
            .ExecuteDeleteAsync(ct);

        await db.MetaAdSetDailyInsights
            .Where(x => x.TenantId == tenantId
                && x.Date >= since && x.Date <= until)
            .ExecuteDeleteAsync(ct);

        await db.MetaAdDailyInsights
            .Where(x => x.TenantId == tenantId
                && x.Date >= since && x.Date <= until)
            .ExecuteDeleteAsync(ct);

        var now = DateTime.UtcNow;

        var campaignEntities = campaignData.Select(d => MapCampaign(d, tenantId, now)).ToList();

        db.MetaCampaignDailyInsights.AddRange(campaignEntities);
        db.MetaAdSetDailyInsights.AddRange(adSetData.Select(d => MapAdSet(d, tenantId, now)));
        db.MetaAdDailyInsights.AddRange(adData.Select(d => MapAd(d, tenantId, now)));

        // Apaga e regrava o resumo diário (1 linha por dia — agrega todas as campanhas)
        await db.DailyAdSpendSummaries
            .Where(x => x.TenantId == tenantId && x.Date >= since && x.Date <= until)
            .ExecuteDeleteAsync(ct);

        var dailySummaries = campaignEntities
            .GroupBy(c => c.Date)
            .Select(g => new DailyAdSpendSummary
            {
                TenantId         = tenantId,
                Date             = g.Key,
                TotalSpend       = g.Sum(c => c.Spend),
                TotalImpressions = g.Sum(c => c.Impressions),
                TotalClicks      = g.Sum(c => c.Clicks),
                TotalConversions = g.Sum(c => c.Conversions),
                TotalRevenue     = g.Sum(c => c.Revenue),
                LastSyncedAt     = now,
            });

        db.DailyAdSpendSummaries.AddRange(dailySummaries);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation(
            "Meta Ads sync concluído — {Campaigns} campanhas, {AdSets} conjuntos, {Ads} anúncios (linhas/dia)",
            campaignData.Count, adSetData.Count, adData.Count);

        return new MetaSyncSummary(campaignData.Count, adSetData.Count, adData.Count, since, until);
    }

    private async Task<(List<MetaInsightData> campaigns, List<MetaInsightData> adSets, List<MetaInsightData> ads)>
        FetchAllLevelsAsync(DateOnly since, DateOnly until, CancellationToken ct)
    {
        // Sequencial para não sobrecarregar rate limit da API
        var campaigns = await client.GetDailyInsightsByLevelAsync(MetaInsightLevel.Campaign, since, until, ct);
        var adSets    = await client.GetDailyInsightsByLevelAsync(MetaInsightLevel.AdSet,    since, until, ct);
        var ads       = await client.GetDailyInsightsByLevelAsync(MetaInsightLevel.Ad,       since, until, ct);
        return (campaigns, adSets, ads);
    }

    private MetaCampaignDailyInsight MapCampaign(MetaInsightData d, Guid tenantId, DateTime now) =>
        new()
        {
            TenantId     = tenantId,
            Date         = ParseDate(d.DateStart),
            CampaignId   = d.CampaignId ?? string.Empty,
            CampaignName = d.CampaignName ?? string.Empty,
            Impressions  = ParseLong(d.Impressions),
            Clicks       = ParseLong(d.Clicks),
            Reach        = ParseLong(d.Reach),
            Spend        = ParseDecimal(d.Spend),
            Cpc          = ParseNullable(d.Cpc),
            Cpm          = ParseNullable(d.Cpm),
            Ctr          = ParseNullable(d.Ctr),
            Frequency    = ParseNullable(d.Frequency),
            Conversions  = SumResults(d.Actions),
            Revenue      = SumRevenue(d.ActionValues),
            LastSyncedAt = now
        };

    private MetaAdSetDailyInsight MapAdSet(MetaInsightData d, Guid tenantId, DateTime now) =>
        new()
        {
            TenantId     = tenantId,
            Date         = ParseDate(d.DateStart),
            CampaignId   = d.CampaignId   ?? string.Empty,
            CampaignName = d.CampaignName ?? string.Empty,
            AdSetId      = d.AdSetId      ?? string.Empty,
            AdSetName    = d.AdSetName    ?? string.Empty,
            Impressions  = ParseLong(d.Impressions),
            Clicks       = ParseLong(d.Clicks),
            Reach        = ParseLong(d.Reach),
            Spend        = ParseDecimal(d.Spend),
            Cpc          = ParseNullable(d.Cpc),
            Cpm          = ParseNullable(d.Cpm),
            Ctr          = ParseNullable(d.Ctr),
            Frequency    = ParseNullable(d.Frequency),
            Conversions  = SumResults(d.Actions),
            Revenue      = SumRevenue(d.ActionValues),
            LastSyncedAt = now
        };

    private MetaAdDailyInsight MapAd(MetaInsightData d, Guid tenantId, DateTime now) =>
        new()
        {
            TenantId     = tenantId,
            Date         = ParseDate(d.DateStart),
            CampaignId   = d.CampaignId   ?? string.Empty,
            CampaignName = d.CampaignName ?? string.Empty,
            AdSetId      = d.AdSetId      ?? string.Empty,
            AdSetName    = d.AdSetName    ?? string.Empty,
            AdId         = d.AdId         ?? string.Empty,
            AdName       = d.AdName       ?? string.Empty,
            Impressions  = ParseLong(d.Impressions),
            Clicks       = ParseLong(d.Clicks),
            Reach        = ParseLong(d.Reach),
            Spend        = ParseDecimal(d.Spend),
            Cpc          = ParseNullable(d.Cpc),
            Cpm          = ParseNullable(d.Cpm),
            Ctr          = ParseNullable(d.Ctr),
            Frequency    = ParseNullable(d.Frequency),
            Conversions  = SumResults(d.Actions),
            Revenue      = SumRevenue(d.ActionValues),
            LastSyncedAt = now
        };

    private static int SumResults(List<MetaAction>? actions)
    {
        if (actions is null) return 0;
        return actions
            .Where(a => a.ActionType == PurchaseActionType)
            .Sum(a => int.TryParse(a.Value, out var v) ? v : 0);
    }

    private static decimal SumRevenue(List<MetaAction>? actionValues)
    {
        if (actionValues is null) return 0m;
        return actionValues
            .Where(a => a.ActionType == PurchaseActionType)
            .Sum(a => decimal.TryParse(a.Value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0m);
    }

    private static DateOnly ParseDate(string value) =>
        DateOnly.TryParse(value, out var d) ? d : DateOnly.FromDateTime(DateTime.UtcNow);

    private static long ParseLong(string? v) =>
        long.TryParse(v, out var r) ? r : 0;

    private static decimal ParseDecimal(string? v) =>
        decimal.TryParse(v, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : 0m;

    private static decimal? ParseNullable(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : ParseDecimal(v);
}
