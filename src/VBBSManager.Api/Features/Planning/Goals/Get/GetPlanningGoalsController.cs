using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Planning.Goals.Get;

[ApiController]
[Route("api/planning")]
public class GetPlanningGoalsController(IGetPlanningGoalsService service) : ControllerBase
{
    [HttpGet("goals")]
    [ProducesResponseType(typeof(PlanningGoalsListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGoals(CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, ct);
        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.Error });
        return Ok(result.Value);
    }
}
