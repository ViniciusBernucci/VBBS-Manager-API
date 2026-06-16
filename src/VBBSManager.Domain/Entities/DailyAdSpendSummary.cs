namespace VBBSManager.Domain.Entities;

public class DailyAdSpendSummary : BaseEntity
{
    public DateOnly Date { get; set; }
    public decimal TotalSpend { get; set; }
    public long TotalImpressions { get; set; }
    public long TotalClicks { get; set; }
    public int TotalConversions { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
