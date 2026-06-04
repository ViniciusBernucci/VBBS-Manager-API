using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Alerts.List;

[ApiController]
[Route("api/alerts")]
[Authorize]
public class AlertsListController(IAlertsListService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AlertsListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] bool onlyUnread = false,
        CancellationToken ct = default)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, onlyUnread, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
