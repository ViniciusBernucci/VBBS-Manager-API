namespace VBBSManager.Domain.Entities;

public class MetaCampaignDailyInsight : BaseEntity
{
    public DateOnly Date { get; set; }
    public string CampaignId { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public long Impressions { get; set; }
    public long Clicks { get; set; }
    public long Reach { get; set; }
    public decimal Spend { get; set; }
    public decimal? Cpc { get; set; }
    public decimal? Cpm { get; set; }
    public decimal? Ctr { get; set; }
    public decimal? Frequency { get; set; }
    public int Conversions { get; set; }
    public decimal Revenue { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
