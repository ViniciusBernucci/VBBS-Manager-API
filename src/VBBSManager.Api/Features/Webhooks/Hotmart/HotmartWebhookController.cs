using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace VBBSManager.Api.Features.Webhooks.Hotmart;

[ApiController]
[Route("api/webhooks")]
public class HotmartWebhookController(IHotmartWebhookService service, ILogger<HotmartWebhookController> logger) : ControllerBase
{
    [HttpPost("hotmart")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Receive(
        [FromBody] JsonDocument payload,
        [FromHeader(Name = "X-Hotmart-Signature")] string? signature,
        CancellationToken ct)
    {
        logger.LogInformation("Hotmart webhook received");

        var result = await service.ProcessAsync(payload, signature, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Hotmart webhook rejected: {Error}", result.Error);
            return BadRequest();
        }

        return Ok();
    }
}
