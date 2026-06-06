using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Financial.FixedExpenses.MonthlyStatus;

[ApiController]
[Route("api/financial/fixed-expenses")]
[Authorize]
public class GetMonthlyStatusController(IGetMonthlyStatusService service) : ControllerBase
{
    [HttpGet("monthly-status")]
    [ProducesResponseType(typeof(List<MonthlyFixedExpenseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, year, month, ct);
        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.Error });
        return Ok(result.Value);
    }
}
