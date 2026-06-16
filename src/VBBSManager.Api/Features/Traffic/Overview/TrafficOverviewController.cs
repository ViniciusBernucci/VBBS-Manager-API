using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Traffic.Overview;

[Authorize]
[ApiController]
[Route("api/traffic")]
public class TrafficOverviewController(ITrafficOverviewService service) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] string since,
        [FromQuery] string until,
        [FromQuery] string[]? campaignIds,
        CancellationToken ct)
    {
        if (!DateOnly.TryParse(since, out var sinceDate) ||
            !DateOnly.TryParse(until, out var untilDate))
            return BadRequest(new { error = "Datas inválidas. Use formato yyyy-MM-dd." });

        if (untilDate < sinceDate)
            return BadRequest(new { error = "A data 'until' deve ser maior ou igual a 'since'." });

        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, sinceDate, untilDate, campaignIds, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
