using System.Text.Json.Serialization;

namespace VBBSManager.Infrastructure.ExternalClients.Meta;

// DTO unificado para todos os 3 níveis (campaign, adset, ad)
// Campos irrelevantes ao nível retornam null
public record MetaInsightData(
    [property: JsonPropertyName("campaign_id")]   string? CampaignId,
    [property: JsonPropertyName("campaign_name")] string? CampaignName,
    [property: JsonPropertyName("adset_id")]      string? AdSetId,
    [property: JsonPropertyName("adset_name")]    string? AdSetName,
    [property: JsonPropertyName("ad_id")]         string? AdId,
    [property: JsonPropertyName("ad_name")]       string? AdName,
    [property: JsonPropertyName("impressions")]   string? Impressions,
    [property: JsonPropertyName("clicks")]        string? Clicks,
    [property: JsonPropertyName("reach")]         string? Reach,
    [property: JsonPropertyName("spend")]         string? Spend,
    [property: JsonPropertyName("cpc")]           string? Cpc,
    [property: JsonPropertyName("cpm")]           string? Cpm,
    [property: JsonPropertyName("ctr")]           string? Ctr,
    [property: JsonPropertyName("frequency")]     string? Frequency,
    [property: JsonPropertyName("actions")]       List<MetaAction>? Actions,
    [property: JsonPropertyName("action_values")] List<MetaAction>? ActionValues,
    [property: JsonPropertyName("date_start")]    string DateStart,
    [property: JsonPropertyName("date_stop")]     string DateStop);

public record MetaAction(
    [property: JsonPropertyName("action_type")] string ActionType,
    [property: JsonPropertyName("value")]       string Value);

public record MetaInsightsResponse(
    [property: JsonPropertyName("data")]   List<MetaInsightData>? Data,
    [property: JsonPropertyName("paging")] MetaPaging? Paging);

public record MetaPaging(
    [property: JsonPropertyName("cursors")] MetaCursors? Cursors,
    [property: JsonPropertyName("next")]    string? Next);

public record MetaCursors(
    [property: JsonPropertyName("before")] string? Before,
    [property: JsonPropertyName("after")]  string? After);

public record MetaApiError(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("type")]    string? Type,
    [property: JsonPropertyName("code")]    int Code);

public record MetaApiErrorResponse(
    [property: JsonPropertyName("error")] MetaApiError? Error);
