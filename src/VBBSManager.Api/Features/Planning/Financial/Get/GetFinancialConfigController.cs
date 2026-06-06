using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Planning.Financial.Get;

[ApiController]
[Route("api/planning")]
public class GetFinancialConfigController(IGetFinancialConfigService service) : ControllerBase
{
    [HttpGet("financial-config")]
    [ProducesResponseType(typeof(FinancialConfigResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, ct);
        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.Error });
        return Ok(result.Value);
    }
}
