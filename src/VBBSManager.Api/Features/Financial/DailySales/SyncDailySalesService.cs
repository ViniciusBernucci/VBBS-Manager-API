using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VBBSManager.Api.Common.Results;
using VBBSManager.Domain.Entities;
using VBBSManager.Infrastructure.ExternalClients.Hotmart;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.DailySales;

public interface ISyncDailySalesService
{
    Task<Result<DailySalesResponse>> ExecuteAsync(Guid tenantId, DateOnly? date, CancellationToken ct);
}

public class SyncDailySalesService(
    AppDbContext db,
    IHotmartSalesService hotmartSalesService,
    ILogger<SyncDailySalesService> logger) : ISyncDailySalesService
{
    public async Task<Result<DailySalesResponse>> ExecuteAsync(Guid tenantId, DateOnly? date, CancellationToken ct)
    {
        var today = date ?? TodaySaoPaulo();
        var (startDate, endDate) = SaoPauloDayToUtc(today);

        logger.LogInformation(
            "Syncing Hotmart daily sales for tenant {TenantId}, date {Date}",
            tenantId, today);

        SalesConsolidatedReport report;
        try
        {
            report = await hotmartSalesService.GetConsolidatedReportAsync(startDate, endDate, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Hotmart sync failed for tenant {TenantId}", tenantId);
            return Result<DailySalesResponse>.Fail(
                "Não foi possível conectar à API da Hotmart. Verifique as credenciais.");
        }

        // Usa valores reais da API: base = produto sem taxa cartão, total = taxa Hotmart real
        var grossRevenue     = report.TotalRevenueByCurrency.TryGetValue("BRL", out var brl)
            ? brl : report.TotalRevenueByCurrency.Values.Sum();
        var hotmartFeeAmount = report.TotalHotmartFeeByCurrency.TryGetValue("BRL", out var fee)
            ? fee : report.TotalHotmartFeeByCurrency.Values.Sum();

        // Taxa fixa adicional de R$ 0,59 por transação cobrada pela Hotmart
        const decimal HotmartFixedFeePerSale = 0.59m;
        hotmartFeeAmount += HotmartFixedFeePerSale * report.TotalSales;

        var netRevenue = grossRevenue - hotmartFeeAmount;
        var now = DateTime.UtcNow;

        var existing = await db.DailySalesSummaries
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Date == today, ct);

        if (existing is not null)
        {
            existing.TotalSales = report.TotalSales;
            existing.GrossRevenue = grossRevenue;
            existing.NetRevenue = netRevenue;
            existing.HotmartFeeAmount = hotmartFeeAmount;
            existing.LastSyncedAt = now;
            existing.UpdatedAt = now;
        }
        else
        {
            db.DailySalesSummaries.Add(new DailySalesSummary
            {
                TenantId = tenantId,
                Date = today,
                TotalSales = report.TotalSales,
                GrossRevenue = grossRevenue,
                NetRevenue = netRevenue,
                HotmartFeeAmount = hotmartFeeAmount,
                LastSyncedAt = now,
            });
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Hotmart sync complete for tenant {TenantId}: {TotalSales} sales, gross {GrossRevenue}",
            tenantId, report.TotalSales, grossRevenue);

        return Result<DailySalesResponse>.Ok(
            new DailySalesResponse(today, report.TotalSales, grossRevenue, netRevenue, hotmartFeeAmount, now));
    }

    // São Paulo = UTC-3. Brasil não adota horário de verão desde 2019.
    private static DateOnly TodaySaoPaulo()
        => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));

    private static (DateTime utcStart, DateTime utcEnd) SaoPauloDayToUtc(DateOnly date)
    {
        // 00:00 SP = 03:00 UTC  |  23:59:59 SP = (dia seguinte) 02:59:59 UTC
        var start = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue).AddHours(3), DateTimeKind.Utc);
        var end   = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MaxValue).AddHours(3), DateTimeKind.Utc);
        return (start, end);
    }
}
