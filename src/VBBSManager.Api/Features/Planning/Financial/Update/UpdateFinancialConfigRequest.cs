namespace VBBSManager.Api.Features.Planning.Financial.Update;

public record UpdateFinancialConfigRequest(
    decimal MonthlyGrossRevenue,
    decimal MonthlyAdSpend,
    decimal HotmartFeePercent,
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
