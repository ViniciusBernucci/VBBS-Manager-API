namespace VBBSManager.Api.Features.Dashboard.Overview;

public record DashboardOverviewResponse(
    int Year,
    int Month,
    bool HasData,
    decimal GrossRevenue,
    decimal OperationalProfit,
    decimal MarginPercentage,
    int TotalSales,
    decimal AdSpend,
    decimal Cpa,
    // Metas prorateadas (dias corridos do mês atual; mês passado = meta cheia)
    decimal TargetGrossRevenue,
    int TargetMonthlySales,
    decimal TargetCpa,
    IReadOnlyList<DashboardWeekPoint> WeeklyEvolution
);

public record DashboardWeekPoint(string Label, decimal Revenue, decimal AdSpend, decimal Margin);
