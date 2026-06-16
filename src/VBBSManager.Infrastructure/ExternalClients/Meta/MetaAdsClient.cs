using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VBBSManager.Infrastructure.ExternalClients.Meta;

public static class MetaInsightLevel
{
    public const string Campaign = "campaign";
    public const string AdSet    = "adset";
    public const string Ad       = "ad";
}

public interface IMetaAdsClient
{
    /// <summary>
    /// Busca insights diários por nível (campaign/adset/ad) para o período informado.
    /// Usa time_increment=1 — retorna um registro por entidade por dia.
    /// </summary>
    Task<List<MetaInsightData>> GetDailyInsightsByLevelAsync(
        string level,
        DateOnly since,
        DateOnly until,
        CancellationToken ct = default);
}

public class MetaAdsClient(
    HttpClient httpClient,
    IOptions<MetaSettings> options,
    ILogger<MetaAdsClient> logger) : IMetaAdsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Campos base comuns a todos os níveis
    private const string BaseFields =
        "campaign_id,campaign_name,impressions,clicks,reach,spend,cpc,cpm,ctr,frequency,actions,action_values";

    // Campos adicionais por nível
    private static readonly Dictionary<string, string> LevelFields = new()
    {
        [MetaInsightLevel.Campaign] = BaseFields,
        [MetaInsightLevel.AdSet]    = $"adset_id,adset_name,{BaseFields}",
        [MetaInsightLevel.Ad]       = $"adset_id,adset_name,ad_id,ad_name,{BaseFields}",
    };

    private readonly MetaSettings _settings = options.Value;

    public Task<List<MetaInsightData>> GetDailyInsightsByLevelAsync(
        string level, DateOnly since, DateOnly until, CancellationToken ct = default)
    {
        if (!LevelFields.ContainsKey(level))
            throw new ArgumentException($"Nível inválido: {level}. Use campaign, adset ou ad.", nameof(level));

        var timeRange = JsonSerializer.Serialize(new
        {
            since = since.ToString("yyyy-MM-dd"),
            until = until.ToString("yyyy-MM-dd")
        });

        return FetchAllAsync(level, timeRange, ct);
    }

    private async Task<List<MetaInsightData>> FetchAllAsync(
        string level, string timeRange, CancellationToken ct)
    {
        var all = new List<MetaInsightData>();
        string? afterCursor = null;

        while (true)
        {
            var query = BuildQuery(level, timeRange, afterCursor);
            var url = $"{_settings.AdAccountId}/insights?{query}";

            logger.LogInformation(
                "Meta Ads GET insights level={Level} cursor={HasCursor}",
                level, afterCursor is not null);

            var response = await httpClient.GetAsync(url, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                ThrowApiError(content, (int)response.StatusCode);

            var page = JsonSerializer.Deserialize<MetaInsightsResponse>(content, JsonOptions)
                ?? throw new InvalidOperationException("Resposta de insights Meta Ads inválida.");

            all.AddRange(page.Data ?? []);

            afterCursor = page.Paging?.Next is not null
                ? page.Paging.Cursors?.After
                : null;

            if (afterCursor is null) break;
        }

        logger.LogInformation(
            "Meta Ads insights [{Level}]: {Count} registros recebidos", level, all.Count);

        return all;
    }

    private string BuildQuery(string level, string timeRange, string? afterCursor)
    {
        var parts = new List<string>
        {
            $"access_token={Uri.EscapeDataString(_settings.AccessToken)}",
            $"level={level}",
            $"fields={Uri.EscapeDataString(LevelFields[level])}",
            $"time_range={Uri.EscapeDataString(timeRange)}",
            "time_increment=1",
            "action_report_time=conversion"   // atribui conversões à data da compra, não do clique
        };

        if (afterCursor is not null)
            parts.Add($"after={Uri.EscapeDataString(afterCursor)}");

        return string.Join("&", parts);
    }

    private static void ThrowApiError(string content, int httpStatus)
    {
        MetaApiErrorResponse? errorResponse = null;
        try { errorResponse = JsonSerializer.Deserialize<MetaApiErrorResponse>(content, JsonOptions); }
        catch { /* ignora falha de parse */ }

        var code = errorResponse?.Error?.Code ?? 0;
        var message = errorResponse?.Error?.Message ?? $"HTTP {httpStatus}: {content}";

        throw code switch
        {
            190           => new MetaTokenException($"Token Meta expirado ou inválido: {message}"),
            200           => new MetaPermissionException($"Permissão insuficiente (ads_read necessário): {message}"),
            4 or 17 or 32 or 613 => new MetaRateLimitException($"Rate limit Meta (código {code}): {message}"),
            _             => new MetaApiException(code, $"Erro Meta API (HTTP {httpStatus}, código {code}): {message}")
        };
    }
}
