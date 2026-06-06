using Microsoft.EntityFrameworkCore;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.DailySales;

public interface IGetDailySalesService
{
    Task<DailySalesResponse?> ExecuteAsync(Guid tenantId, CancellationToken ct);
}

public class GetDailySalesService(AppDbContext db) : IGetDailySalesService
{
    public async Task<DailySalesResponse?> ExecuteAsync(Guid tenantId, CancellationToken ct)
    {
        // São Paulo = UTC-3. Brasil não adota horário de verão desde 2019.
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));

        var record = await db.DailySalesSummaries
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Date == today, ct);

        if (record is null)
            return null;

        return new DailySalesResponse(
            record.Date,
            record.TotalSales,
            record.GrossRevenue,
            record.NetRevenue,
            record.HotmartFeeAmount,
            record.LastSyncedAt);
    }
}
