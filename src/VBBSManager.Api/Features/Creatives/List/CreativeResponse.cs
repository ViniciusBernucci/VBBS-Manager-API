namespace VBBSManager.Api.Features.Creatives.List;

public record CreativesListResponse(IReadOnlyList<CreativeItem> Items);

public record CreativeItem(
    string ExternalId,
    string Name,
    decimal Spend,
    decimal Cpa,
    decimal Ctr,
    int Conversions,
    string Semaphore,
    DateOnly Date
);
