using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Financial.DRE;

[ApiController]
[Route("api/financial")]
[Authorize]
public class DreController(IDreService service) : ControllerBase
{
    [HttpGet("dre")]
    [ProducesResponseType(typeof(DreResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDre(
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
