using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Dashboard.Overview;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardOverviewController(IDashboardOverviewService service) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(DashboardOverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, year, month, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
