using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Creatives.List;

[ApiController]
[Route("api/creatives")]
[Authorize]
public class CreativesListController(ICreativesListService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CreativesListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCreatives(
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
