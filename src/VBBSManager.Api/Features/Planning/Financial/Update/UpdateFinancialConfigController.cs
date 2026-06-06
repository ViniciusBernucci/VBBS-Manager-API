using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Planning.Financial.Update;

[ApiController]
[Route("api/planning")]
public class UpdateFinancialConfigController(IUpdateFinancialConfigService service) : ControllerBase
{
    [HttpPut("financial-config")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateFinancialConfigRequest request, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, request, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return NoContent();
    }
}
