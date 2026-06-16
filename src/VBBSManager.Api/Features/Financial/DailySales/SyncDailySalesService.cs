using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VBBSManager.Api.Common.Results;
using VBBSManager.Domain.Entities;
using VBBSManager.Infrastructure.ExternalClients.Hotmart;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.DailySales;

public interface ISyncDailySalesService
{
    Task<Result<DailySalesSyncResponse>> ExecuteAsync(Guid tenantId, int year, int month, CancellationToken ct);
}

public class SyncDailySalesService(
    AppDbContext db,
    IHotmartSalesService hotmartSalesService,
    ILogger<SyncDailySalesService> logger) : ISyncDailySalesService
{
    public async Task<Result<DailySalesSyncResponse>> ExecuteAsync(
        Guid tenantId, int year, int month, CancellationToken ct)
    {
        if (year < 2020 || year > DateTime.UtcNow.Year + 1)
            return Result<DailySalesSyncResponse>.Fail("Ano inválido.");

        if (month < 1 || month > 12)
            return Result<DailySalesSyncResponse>.Fail("Mês inválido (1–12).");

        var since = new DateOnly(year, month, 1);
        var until = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        // Hotmart API range: cover full month in São Paulo time (UTC-3)
        // Month start SP midnight = UTC+3h; month end SP 23:59 = next day UTC 02:59
        var utcStart = DateTime.SpecifyKind(since.ToDateTime(TimeOnly.MinValue).AddHours(3), DateTimeKind.Utc);
        var utcEnd   = DateTime.SpecifyKind(until.ToDateTime(TimeOnly.MaxValue).AddHours(3), DateTimeKind.Utc);

        logger.LogInformation(
            "Hotmart month sync — tenant {TenantId}, {Year}/{Month:D2}",
            tenantId, year, month);

        List<DailySaleData> dailySales;
        try
        {
            dailySales = await hotmartSalesService.GetSalesByDayAsync(utcStart, utcEnd, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Hotmart sync failed for tenant {TenantId}", tenantId);
            return Result<DailySalesSyncResponse>.Fail(
                "Não foi possível conectar à API da Hotmart. Verifique as credenciais.");
        }

        var now = DateTime.UtcNow;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // Delete all records for this month and reinsert
        await db.DailySalesSummaries
            .Where(x => x.TenantId == tenantId && x.Date >= since && x.Date <= until)
            .ExecuteDeleteAsync(ct);

        foreach (var day in dailySales)
        {
            // Only sync days within the requested month (API might return edge cases)
            if (day.Date < since || day.Date > until) continue;

            // hotmart_fee.total já inclui taxa fixa + taxa percentual — sem adição extra
            var hotmartFee = day.HotmartFeeAmount;
            var netRevenue = day.GrossRevenue - hotmartFee;

            db.DailySalesSummaries.Add(new DailySalesSummary
            {
                TenantId         = tenantId,
                Date             = day.Date,
                TotalSales       = day.TotalSales,
                GrossRevenue     = day.GrossRevenue,
                NetRevenue       = netRevenue,
                HotmartFeeAmount = hotmartFee,
                LastSyncedAt     = now,
            });
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var totalSales = dailySales.Sum(x => x.TotalSales);
        var days       = dailySales.Count;

        logger.LogInformation(
            "Hotmart sync complete — {Days} days, {TotalSales} total sales ({Since} a {Until})",
            days, totalSales, since, until);

        return Result<DailySalesSyncResponse>.Ok(new DailySalesSyncResponse(
            DaysSynced: days,
            TotalSales: totalSales,
            Since:      since.ToString("yyyy-MM-dd"),
            Until:      until.ToString("yyyy-MM-dd"),
            Message:    $"Sincronizado: {days} dias com {totalSales} venda(s) no total"));
    }
}
