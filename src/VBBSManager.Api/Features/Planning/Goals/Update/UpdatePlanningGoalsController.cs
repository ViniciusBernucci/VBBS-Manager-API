using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Planning.Goals.Update;

[ApiController]
[Route("api/planning")]
public class UpdatePlanningGoalsController(IUpdatePlanningGoalsService service) : ControllerBase
{
    [HttpPut("goals")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateGoals([FromBody] UpdatePlanningGoalsRequest request, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, request, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return NoContent();
    }
}
