using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace VBBSManager.Api.Features.Webhooks.Brevo;

[ApiController]
[Route("api/webhooks")]
public class BrevoWebhookController(IBrevoWebhookService service, ILogger<BrevoWebhookController> logger) : ControllerBase
{
    [HttpPost("brevo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Receive([FromBody] JsonDocument payload, CancellationToken ct)
    {
        logger.LogInformation("Brevo webhook received");

        var result = await service.ProcessAsync(payload, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Brevo webhook rejected: {Error}", result.Error);
            return BadRequest();
        }

        return Ok();
    }
}
