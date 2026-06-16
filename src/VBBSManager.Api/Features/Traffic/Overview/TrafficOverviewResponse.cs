namespace VBBSManager.Api.Features.Traffic.Overview;

public record TrafficOverviewResponse(
    string Since,
    string Until,
    bool HasData,
    TrafficKpis Kpis,
    IReadOnlyList<DailyStatPoint> DailyStats,
    IReadOnlyList<AdInsightRow> Ads,
    IReadOnlyList<AdSetInsightRow> AdSets,
    IReadOnlyList<CampaignInsightRow> Campaigns);

public record TrafficKpis(
    decimal TotalSpend,
    int TotalConversions,
    decimal Cpa,
    decimal AvgFrequency,
    decimal AvgCtr,
    decimal AvgCpm);

public record DailyStatPoint(string Date, decimal Spend, int Conversions, decimal Revenue);

public record AdInsightRow(
    string AdId,
    string AdName,
    string AdSetName,
    string CampaignName,
    decimal Spend,
    int Conversions,
    decimal Cpa,
    decimal Ctr,
    decimal Cpm,
    decimal Frequency,
    string Status);

public record AdSetInsightRow(
    string AdSetId,
    string AdSetName,
    string CampaignName,
    decimal Spend,
    int Conversions,
    decimal Cpa,
    decimal Ctr,
    decimal Cpm,
    decimal Frequency);

public record CampaignInsightRow(
    string CampaignId,
    string CampaignName,
    decimal Spend,
    int Conversions,
    decimal Cpa,
    decimal Ctr,
    decimal Cpm,
    decimal Frequency);
