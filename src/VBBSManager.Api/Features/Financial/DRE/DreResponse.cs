namespace VBBSManager.Api.Features.Financial.DRE;

public record DreResponse(
    decimal GrossRevenue,
    decimal HotmartFee,
    decimal AdSpend,
    decimal OtherExpenses,
    decimal NetRevenue,
    decimal EstimatedMargin,
    decimal MarginPercentage,
    decimal MonthProjection,
    IReadOnlyList<DreDataPoint> WeeklyEvolution
);

public record DreDataPoint(DateOnly Date, decimal Revenue, decimal AdSpend, decimal Margin);
