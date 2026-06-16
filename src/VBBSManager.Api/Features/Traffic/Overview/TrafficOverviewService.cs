using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Traffic.Overview;

public interface ITrafficOverviewService
{
    Task<Result<TrafficOverviewResponse>> ExecuteAsync(
        Guid tenantId, DateOnly since, DateOnly until, string[]? campaignIds, CancellationToken ct);
}

public class TrafficOverviewService(
    AppDbContext db,
    ILogger<TrafficOverviewService> logger) : ITrafficOverviewService
{
    // Thresholds de semáforo por CPA — TODO: tornar configurável por tenant
    private const decimal GreenMax  = 45m;
    private const decimal YellowMax = 70m;

    public async Task<Result<TrafficOverviewResponse>> ExecuteAsync(
        Guid tenantId, DateOnly since, DateOnly until, string[]? campaignIds, CancellationToken ct)
    {
        var filtered = campaignIds is { Length: > 0 };

        logger.LogInformation(
            "Traffic overview — tenant {TenantId} {Since} a {Until} filtro={Filter}",
            tenantId, since, until, filtered ? string.Join(",", campaignIds!) : "todos");

        var campaignQuery = db.MetaCampaignDailyInsights
            .Where(x => x.TenantId == tenantId && x.Date >= since && x.Date <= until);

        var adSetQuery = db.MetaAdSetDailyInsights
            .Where(x => x.TenantId == tenantId && x.Date >= since && x.Date <= until);

        var adQuery = db.MetaAdDailyInsights
            .Where(x => x.TenantId == tenantId && x.Date >= since && x.Date <= until);

        if (filtered)
        {
            campaignQuery = campaignQuery.Where(x => campaignIds!.Contains(x.CampaignId));
            adSetQuery    = adSetQuery.Where(x => campaignIds!.Contains(x.CampaignId));
            adQuery       = adQuery.Where(x => campaignIds!.Contains(x.CampaignId));
        }

        var campaignRows = await campaignQuery.AsNoTracking().ToListAsync(ct);
        var adSetRows    = await adSetQuery.AsNoTracking().ToListAsync(ct);
        var adRows       = await adQuery.AsNoTracking().ToListAsync(ct);

        var hasData = campaignRows.Count > 0;

        // KPIs — calculados a partir do nível de campanha (mais preciso, sem sobreposição)
        var totalSpend       = campaignRows.Sum(x => x.Spend);
        var totalConversions = campaignRows.Sum(x => x.Conversions);
        var totalImpressions = campaignRows.Sum(x => x.Impressions);
        var totalClicks      = campaignRows.Sum(x => x.Clicks);
        var cpa              = totalConversions > 0 ? totalSpend / totalConversions : 0m;
        var avgFrequency     = campaignRows.Count > 0
            ? campaignRows.Where(x => x.Frequency.HasValue).Select(x => x.Frequency!.Value).DefaultIfEmpty(0).Average()
            : 0m;
        var avgCtr = totalImpressions > 0 ? (decimal)totalClicks / totalImpressions * 100m : 0m;
        var avgCpm = totalImpressions > 0 ? totalSpend / totalImpressions * 1000m : 0m;

        // Gráfico diário
        var dailyStats = campaignRows
            .GroupBy(x => x.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyStatPoint(
                g.Key.ToString("yyyy-MM-dd"),
                g.Sum(x => x.Spend),
                g.Sum(x => x.Conversions),
                g.Sum(x => x.Revenue)))
            .ToList();

        // Nível campanha — agrupado
        var campaigns = campaignRows
            .GroupBy(x => new { x.CampaignId, x.CampaignName })
            .Select(g =>
            {
                var spend       = g.Sum(x => x.Spend);
                var conversions = g.Sum(x => x.Conversions);
                var impr        = g.Sum(x => x.Impressions);
                var clicks      = g.Sum(x => x.Clicks);
                return new CampaignInsightRow(
                    g.Key.CampaignId,
                    g.Key.CampaignName,
                    Spend:       spend,
                    Conversions: conversions,
                    Cpa:         conversions > 0 ? spend / conversions : 0m,
                    Ctr:         impr > 0 ? (decimal)clicks / impr * 100m : 0m,
                    Cpm:         impr > 0 ? spend / impr * 1000m : 0m,
                    Frequency:   (decimal)g.Where(x => x.Frequency.HasValue).Select(x => x.Frequency!.Value).DefaultIfEmpty(0).Average());
            })
            .OrderByDescending(x => x.Spend)
            .ToList();

        // Nível conjunto de anúncios
        var adSets = adSetRows
            .GroupBy(x => new { x.AdSetId, x.AdSetName, x.CampaignName })
            .Select(g =>
            {
                var spend       = g.Sum(x => x.Spend);
                var conversions = g.Sum(x => x.Conversions);
                var impr        = g.Sum(x => x.Impressions);
                var clicks      = g.Sum(x => x.Clicks);
                return new AdSetInsightRow(
                    g.Key.AdSetId,
                    g.Key.AdSetName,
                    g.Key.CampaignName,
                    Spend:       spend,
                    Conversions: conversions,
                    Cpa:         conversions > 0 ? spend / conversions : 0m,
                    Ctr:         impr > 0 ? (decimal)clicks / impr * 100m : 0m,
                    Cpm:         impr > 0 ? spend / impr * 1000m : 0m,
                    Frequency:   (decimal)g.Where(x => x.Frequency.HasValue).Select(x => x.Frequency!.Value).DefaultIfEmpty(0).Average());
            })
            .OrderByDescending(x => x.Spend)
            .ToList();

        // Nível anúncio (criativos)
        var ads = adRows
            .GroupBy(x => new { x.AdId, x.AdName, x.AdSetName, x.CampaignName })
            .Select(g =>
            {
                var spend       = g.Sum(x => x.Spend);
                var conversions = g.Sum(x => x.Conversions);
                var impr        = g.Sum(x => x.Impressions);
                var clicks      = g.Sum(x => x.Clicks);
                var freq        = (decimal)g.Where(x => x.Frequency.HasValue).Select(x => x.Frequency!.Value).DefaultIfEmpty(0).Average();
                var adCpa       = conversions > 0 ? spend / conversions : 0m;
                return new AdInsightRow(
                    g.Key.AdId,
                    g.Key.AdName,
                    g.Key.AdSetName,
                    g.Key.CampaignName,
                    Spend:       spend,
                    Conversions: conversions,
                    Cpa:         adCpa,
                    Ctr:         impr > 0 ? (decimal)clicks / impr * 100m : 0m,
                    Cpm:         impr > 0 ? spend / impr * 1000m : 0m,
                    Frequency:   freq,
                    Status:      Semaphore(adCpa, conversions, spend));
            })
            .OrderByDescending(x => x.Spend)
            .ToList();

        return Result<TrafficOverviewResponse>.Ok(new TrafficOverviewResponse(
            Since:     since.ToString("yyyy-MM-dd"),
            Until:     until.ToString("yyyy-MM-dd"),
            HasData:   hasData,
            Kpis:      new TrafficKpis(totalSpend, totalConversions, cpa,
                           Math.Round(avgFrequency, 2), Math.Round(avgCtr, 2), Math.Round(avgCpm, 2)),
            DailyStats: dailyStats,
            Ads:        ads,
            AdSets:     adSets,
            Campaigns:  campaigns));
    }

    private static string Semaphore(decimal cpa, int conversions, decimal spend)
    {
        if (conversions == 0) return spend > 0m ? "no_result" : "gray";
        return cpa < GreenMax ? "green" : cpa < YellowMax ? "yellow" : "red";
    }
}
