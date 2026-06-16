namespace VBBSManager.Domain.Entities;

public class FinancialConfig : BaseEntity
{
    public decimal MonthlyGrossRevenue { get; set; }
    public decimal MonthlyAdSpend { get; set; }
    public decimal HotmartFeePercent { get; set; }
    public decimal InstallmentFeePercent { get; set; }
    public decimal InstallmentSalesPercent { get; set; }
    public decimal FederalTaxPercent { get; set; }
    public decimal RefundRatePercent { get; set; }
    public decimal MetaAdsTaxPercent { get; set; }
    public decimal AccountingCost { get; set; }
    public decimal InvoicingCost { get; set; }
    public decimal ManychatCost { get; set; }
    public decimal HotmartPlayerCost { get; set; }
    public decimal HotmartFixedFeePerTransaction { get; set; }
}
