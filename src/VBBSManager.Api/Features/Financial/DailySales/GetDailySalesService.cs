using Microsoft.EntityFrameworkCore;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.DailySales;

public interface IGetDailySalesService
{
    Task<DailySalesOverviewResponse> ExecuteAsync(Guid tenantId, DateOnly since, DateOnly until, CancellationToken ct);
}

public class GetDailySalesService(AppDbContext db) : IGetDailySalesService
{
    public async Task<DailySalesOverviewResponse> ExecuteAsync(
        Guid tenantId, DateOnly since, DateOnly until, CancellationToken ct)
    {
        var records = await db.DailySalesSummaries
            .Where(x => x.TenantId == tenantId && x.Date >= since && x.Date <= until)
            .OrderBy(x => x.Date)
            .AsNoTracking()
            .ToListAsync(ct);

        var hasData = records.Count > 0;

        var dailyStats = records
            .Select(r => new DailySalesDailyPoint(
                r.Date.ToString("yyyy-MM-dd"),
                r.TotalSales,
                r.GrossRevenue,
                r.NetRevenue,
                r.HotmartFeeAmount))
            .ToList();

        var lastSync = records.Count > 0
            ? records.Max(r => r.LastSyncedAt).ToString("o")
            : null;

        return new DailySalesOverviewResponse(
            Since:                since.ToString("yyyy-MM-dd"),
            Until:                until.ToString("yyyy-MM-dd"),
            HasData:              hasData,
            TotalSales:           records.Sum(r => r.TotalSales),
            TotalGrossRevenue:    records.Sum(r => r.GrossRevenue),
            TotalNetRevenue:      records.Sum(r => r.NetRevenue),
            TotalHotmartFeeAmount: records.Sum(r => r.HotmartFeeAmount),
            LastSyncedAt:         lastSync,
            DailyStats:           dailyStats);
    }
}
