using System.Text.Json;
using VBBSManager.Api.Common.Results;

namespace VBBSManager.Api.Features.Webhooks.Hotmart;

public interface IHotmartWebhookService
{
    Task<Result> ProcessAsync(JsonDocument payload, string? signature, CancellationToken ct);
}

public class HotmartWebhookService(ILogger<HotmartWebhookService> logger) : IHotmartWebhookService
{
    public async Task<Result> ProcessAsync(JsonDocument payload, string? signature, CancellationToken ct)
    {
        // TODO: validar assinatura HMAC do webhook
        // TODO: identificar tenant pelo produto Hotmart
        // TODO: persistir evento e disparar processamento (purchase_complete, refund, etc.)
        logger.LogInformation("Processing Hotmart webhook payload");

        await Task.CompletedTask;
        return Result.Fail("Not implemented");
    }
}
