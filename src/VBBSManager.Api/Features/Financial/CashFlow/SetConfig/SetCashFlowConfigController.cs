using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Financial.CashFlow.SetConfig;

[ApiController]
[Route("api/financial/cash-flow")]
[Authorize]
public class SetCashFlowConfigController(ISetCashFlowConfigService service) : ControllerBase
{
    [HttpPut("config")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Set([FromBody] SetCashFlowConfigRequest request, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, request, ct);
        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.Error });
        return NoContent();
    }
}
