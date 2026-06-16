namespace VBBSManager.Api.Features.Planning.Financial.Get;

public record FinancialConfigResponse(
    Guid Id,
    decimal MonthlyGrossRevenue,
    decimal MonthlyAdSpend,
    decimal HotmartFeePercent,
    decimal HotmartFixedFeePerTransaction,
    decimal InstallmentFeePercent,
    decimal InstallmentSalesPercent,
    decimal FederalTaxPercent,
    decimal RefundRatePercent,
    decimal MetaAdsTaxPercent,
    decimal AccountingCost,
    decimal InvoicingCost,
    decimal ManychatCost,
    decimal HotmartPlayerCost
);
