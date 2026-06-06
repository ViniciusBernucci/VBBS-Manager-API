using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VBBSManager.Infrastructure.ExternalClients.Hotmart;

public interface IHotmartAuthClient
{
    Task<AuthResponse> GetAccessTokenAsync(CancellationToken ct);
}

public class HotmartAuthClient(
    HttpClient httpClient,
    IOptions<HotmartSettings> settings,
    ILogger<HotmartAuthClient> logger) : IHotmartAuthClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AuthResponse> GetAccessTokenAsync(CancellationToken ct)
    {
        var clientId = settings.Value.ClientId;
        var clientSecret = settings.Value.ClientSecret;

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException(
                "Credenciais Hotmart não configuradas. Defina HOTMART_CLIENT_ID e HOTMART_CLIENT_SECRET no .env.");

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/security/oauth/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        });

        logger.LogInformation("Hotmart API {Method} {Endpoint}", "POST", "/security/oauth/token");

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Resposta de autenticação Hotmart inválida ou vazia.");

        return authResponse;
    }
}
