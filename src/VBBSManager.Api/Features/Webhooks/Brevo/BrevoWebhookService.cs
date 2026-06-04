using System.Text.Json;
using VBBSManager.Api.Common.Results;

namespace VBBSManager.Api.Features.Webhooks.Brevo;

public interface IBrevoWebhookService
{
    Task<Result> ProcessAsync(JsonDocument payload, CancellationToken ct);
}

public class BrevoWebhookService(ILogger<BrevoWebhookService> logger) : IBrevoWebhookService
{
    public async Task<Result> ProcessAsync(JsonDocument payload, CancellationToken ct)
    {
        // TODO: identificar evento (email_opened, clicked, bounce, unsubscribe)
        // TODO: persistir evento linkado ao lead e tenant correto
        logger.LogInformation("Processing Brevo webhook payload");

        await Task.CompletedTask;
        return Result.Fail("Not implemented");
    }
}
