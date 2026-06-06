namespace VBBSManager.Api.Features.Financial.DRE;

public record DreResponse(
    int Year,
    int Month,
    bool HasData,
    DreSummary Summary,
    IReadOnlyList<DreLineDto> Lines,
    IReadOnlyList<DreDataPoint> WeeklyEvolution
);

public record DreSummary(
    decimal GrossRevenue,
    decimal NetRevenue,
    decimal OperationalProfit,
    decimal MarginPercentage,
    decimal MonthProjection
);

public record DreLineDto(
    string Key,
    string Label,
    decimal Amount,
    string Kind,
    decimal? PercentOfGross,
    bool IsEstimated
);

public record DreDataPoint(DateOnly Date, decimal Revenue, decimal AdSpend, decimal Margin);
