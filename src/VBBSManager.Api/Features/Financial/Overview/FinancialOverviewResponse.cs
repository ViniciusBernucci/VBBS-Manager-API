namespace VBBSManager.Api.Features.Financial.Overview;

public record FinancialOverviewResponse(
    decimal GrossRevenue,
    decimal NetRevenue,
    decimal AdSpend,
    decimal EstimatedMargin,
    decimal Roas,
    decimal Cpa,
    decimal AverageTicket,
    int TotalSales,
    decimal GrossRevenueVariation,
    decimal RoasVariation,
    decimal CpaVariation
);
