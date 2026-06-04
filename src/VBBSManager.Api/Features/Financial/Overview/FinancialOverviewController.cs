using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Financial.Overview;

[ApiController]
[Route("api/financial")]
[Authorize]
public class FinancialOverviewController(IFinancialOverviewService service) : ControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(FinancialOverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, from, to, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
