using Microsoft.Extensions.Logging;

namespace VBBSManager.Infrastructure.ExternalClients.Hotmart;

public interface IHotmartClient
{
    Task<HotmartSalesResponse> GetSalesAsync(string accessToken, DateOnly from, DateOnly to, CancellationToken ct);
}

public class HotmartClient(HttpClient httpClient, ILogger<HotmartClient> logger) : IHotmartClient
{
    private const string BaseUrl = "https://developers.hotmart.com/payments/api/v1";

    public async Task<HotmartSalesResponse> GetSalesAsync(string accessToken, DateOnly from, DateOnly to, CancellationToken ct)
    {
        // Polly retry está configurado no HttpClient via DI (ver Extensions/ServiceCollectionExtensions)
        logger.LogInformation("Fetching Hotmart sales from {From} to {To}", from, to);

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"{BaseUrl}/sales/history?start_date={from:yyyy-MM-dd}&end_date={to:yyyy-MM-dd}");
        request.Headers.Authorization = new("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);

        // TODO: deserializar resposta para HotmartSalesResponse
        return new HotmartSalesResponse([]);
    }
}

public record HotmartSalesResponse(IReadOnlyList<HotmartSaleItem> Items);
public record HotmartSaleItem(string TransactionCode, decimal Price, string Status, DateTime ApprovedDate);
