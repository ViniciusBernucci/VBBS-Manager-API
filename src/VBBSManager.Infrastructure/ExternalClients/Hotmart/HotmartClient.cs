using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace VBBSManager.Infrastructure.ExternalClients.Hotmart;

public interface IHotmartClient
{
    Task<HotmartSalesResponse> GetSalesPageAsync(
        string accessToken,
        long startDateMs,
        long endDateMs,
        string? pageToken,
        CancellationToken ct);
}

public class HotmartClient(HttpClient httpClient, ILogger<HotmartClient> logger) : IHotmartClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<HotmartSalesResponse> GetSalesPageAsync(
        string accessToken,
        long startDateMs,
        long endDateMs,
        string? pageToken,
        CancellationToken ct)
    {
        var query = $"start_date={startDateMs}&end_date={endDateMs}";

        if (!string.IsNullOrWhiteSpace(pageToken))
            query += $"&page_token={Uri.EscapeDataString(pageToken)}";

        // Sem barra inicial: resolve relativo ao base address (payments/api/v1/)
        var endpoint = $"sales/history?{query}";

        logger.LogInformation(
            "Hotmart API {Method} {Endpoint} (start={StartMs}, end={EndMs}, hasPageToken={HasPageToken})",
            "GET",
            "/sales/history",
            startDateMs,
            endDateMs,
            !string.IsNullOrWhiteSpace(pageToken));

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var salesResponse = JsonSerializer.Deserialize<HotmartSalesResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Resposta de vendas Hotmart inválida ou vazia.");

        return salesResponse;
    }
}
