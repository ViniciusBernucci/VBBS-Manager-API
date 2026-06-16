namespace VBBSManager.Api.Features.Financial.DailySales;

public record DailySalesDailyPoint(
    string Date,
    int TotalSales,
    decimal GrossRevenue,
    decimal NetRevenue,
    decimal HotmartFeeAmount);

public record DailySalesOverviewResponse(
    string Since,
    string Until,
    bool HasData,
    int TotalSales,
    decimal TotalGrossRevenue,
    decimal TotalNetRevenue,
    decimal TotalHotmartFeeAmount,
    string? LastSyncedAt,
    List<DailySalesDailyPoint> DailyStats);

public record DailySalesSyncRequest(int Year, int Month);

public record DailySalesSyncResponse(
    int DaysSynced,
    int TotalSales,
    string Since,
    string Until,
    string Message);
