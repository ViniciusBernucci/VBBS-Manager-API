using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Creatives.List;

public interface ICreativesListService
{
    Task<Result<CreativesListResponse>> ExecuteAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct);
}

public class CreativesListService(
    AppDbContext db,
    ILogger<CreativesListService> logger) : ICreativesListService
{
    private const decimal CpaGreenMax  = 45m;
    private const decimal CpaYellowMax = 70m;

    public async Task<Result<CreativesListResponse>> ExecuteAsync(
        Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        logger.LogInformation(
            "Creatives list — tenant {TenantId}, {From} a {To}", tenantId, from, to);

        var rows = await db.MetaAdDailyInsights
            .Where(x => x.TenantId == tenantId && x.Date >= from && x.Date <= to)
            .AsNoTracking()
            .ToListAsync(ct);

        if (rows.Count == 0)
            return Result<CreativesListResponse>.Ok(new CreativesListResponse([]));

        var items = rows
            .GroupBy(x => new { x.AdId, x.AdName })
            .Select(g =>
            {
                var spend       = g.Sum(x => x.Spend);
                var conversions = g.Sum(x => x.Conversions);
                var impr        = g.Sum(x => x.Impressions);
                var clicks      = g.Sum(x => x.Clicks);
                var cpa         = conversions > 0 ? spend / conversions : 0m;
                var ctr         = impr > 0 ? (decimal)clicks / impr * 100m : 0m;

                return new CreativeItem(
                    ExternalId:  g.Key.AdId,
                    Name:        g.Key.AdName,
                    Spend:       spend,
                    Cpa:         cpa,
                    Ctr:         Math.Round(ctr, 2),
                    Conversions: conversions,
                    Semaphore:   Semaphore(cpa, conversions),
                    Date:        g.Min(x => x.Date));
            })
            .OrderByDescending(x => x.Spend)
            .ToList();

        return Result<CreativesListResponse>.Ok(new CreativesListResponse(items));
    }

    private static string Semaphore(decimal cpa, int conversions)
    {
        if (conversions == 0) return "gray";
        return cpa < CpaGreenMax ? "green" : cpa < CpaYellowMax ? "yellow" : "red";
    }
}
