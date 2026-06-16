using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Traffic.Sync;

[Authorize]
[ApiController]
[Route("api/traffic")]
public class TrafficSyncController(ITrafficSyncService service) : ControllerBase
{
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(
        [FromBody] TrafficSyncRequest request,
        CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, request.Year, request.Month, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
