namespace VBBSManager.Domain.Entities;

public class DailySalesSummary : BaseEntity
{
    public DateOnly Date { get; set; }
    public int TotalSales { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal HotmartFeeAmount { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
