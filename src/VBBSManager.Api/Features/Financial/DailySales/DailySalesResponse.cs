namespace VBBSManager.Api.Features.Financial.DailySales;

public record DailySalesResponse(
    DateOnly Date,
    int TotalSales,
    decimal GrossRevenue,
    decimal NetRevenue,
    decimal HotmartFeeAmount,
    DateTime LastSyncedAt
);
