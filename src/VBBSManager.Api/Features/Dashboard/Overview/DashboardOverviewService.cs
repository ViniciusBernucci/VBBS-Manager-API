using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Api.Features.Financial.DRE;
using VBBSManager.Api.Features.Traffic.Overview;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Dashboard.Overview;

public interface IDashboardOverviewService
{
    Task<Result<DashboardOverviewResponse>> ExecuteAsync(Guid tenantId, int year, int month, CancellationToken ct);
}

public class DashboardOverviewService(
    IDreService dreService,
    ITrafficOverviewService trafficService,
    AppDbContext db,
    ILogger<DashboardOverviewService> logger) : IDashboardOverviewService
{
    public async Task<Result<DashboardOverviewResponse>> ExecuteAsync(Guid tenantId, int year, int month, CancellationToken ct)
    {
        if (year < 2020 || year > DateTime.UtcNow.Year + 1)
            return Result<DashboardOverviewResponse>.Fail("Ano inválido.");
        if (month < 1 || month > 12)
            return Result<DashboardOverviewResponse>.Fail("Mês inválido (1–12).");

        var since = new DateOnly(year, month, 1);
        var until = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var dreResult = await dreService.ExecuteAsync(tenantId, year, month, ct);
        if (!dreResult.IsSuccess)
            return Result<DashboardOverviewResponse>.Fail(dreResult.Error!);

        var trafficResult = await trafficService.ExecuteAsync(tenantId, since, until, null, ct);

        var config = await db.FinancialConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        var goals = await db.PlanningGoals
            .Where(g => g.TenantId == tenantId)
            .AsNoTracking()
            .ToListAsync(ct);

        var dre = dreResult.Value!;

        var cpa = trafficResult.IsSuccess && trafficResult.Value!.Kpis.TotalConversions > 0
            ? trafficResult.Value.Kpis.Cpa
            : 0m;

        var weeklyPoints = dre.WeeklyEvolution
            .Select((w, i) => new DashboardWeekPoint(
                $"Sem. {(i + 1):D2}",
                w.Revenue,
                w.AdSpend,
                w.Margin))
            .ToList();

        var adSpend = dre.Lines
            .Where(l => l.Key == "ad-spend")
            .Select(l => Math.Abs(l.Amount))
            .FirstOrDefault();

        var (targetRevenue, targetSales, targetCpa) = ComputeTargets(year, month, config, goals);

        logger.LogInformation(
            "Dashboard overview — tenant {TenantId} {Year}/{Month} receita={Revenue} lucro={Profit}",
            tenantId, year, month, dre.Summary.GrossRevenue, dre.Summary.OperationalProfit);

        return Result<DashboardOverviewResponse>.Ok(new DashboardOverviewResponse(
            Year:               year,
            Month:              month,
            HasData:            dre.HasData,
            GrossRevenue:       dre.Summary.GrossRevenue,
            OperationalProfit:  dre.Summary.OperationalProfit,
            MarginPercentage:   dre.Summary.MarginPercentage,
            TotalSales:         dre.Summary.TotalSales,
            AdSpend:            adSpend,
            Cpa:                cpa,
            TargetGrossRevenue: targetRevenue,
            TargetMonthlySales: targetSales,
            TargetCpa:          targetCpa,
            WeeklyEvolution:    weeklyPoints));
    }

    private static (decimal revenue, int sales, decimal cpa) ComputeTargets(
        int year, int month,
        Domain.Entities.FinancialConfig? config,
        List<Domain.Entities.PlanningGoal> goals)
    {
        var monthlyRevTarget   = config?.MonthlyGrossRevenue ?? 0m;
        var weeklySalesTarget  = goals.FirstOrDefault(g => g.Key == "weekly_sales")?.TargetValue ?? 0m;
        var cpaTarget          = goals.FirstOrDefault(g => g.Key == "cpa_general")?.TargetValue ?? 42m;

        var monthlySalesTarget = (int)Math.Round(weeklySalesTarget * 4.33m);

        var today      = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd   = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        // Prorata apenas no mês corrente
        if (today >= monthStart && today <= monthEnd)
        {
            var factor = (decimal)today.Day / monthEnd.Day;
            return (monthlyRevTarget * factor, (int)Math.Round(monthlySalesTarget * factor), cpaTarget);
        }

        return (monthlyRevTarget, monthlySalesTarget, cpaTarget);
    }
}
