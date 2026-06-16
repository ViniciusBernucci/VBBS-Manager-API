namespace VBBSManager.Api.Features.Traffic.Sync;

public record TrafficSyncRequest(int Year, int Month);

public record TrafficSyncResponse(
    int Campaigns,
    int AdSets,
    int Ads,
    string Since,
    string Until,
    string Message);
