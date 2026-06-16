using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Financial.DailySales;

[ApiController]
[Route("api/financial/daily-sales")]
[Authorize]
public class DailySalesController(
    IGetDailySalesService getService,
    ISyncDailySalesService syncService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(DailySalesOverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? since,
        [FromQuery] DateOnly? until,
        CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));
        var resolvedSince = since ?? new DateOnly(today.Year, today.Month, 1);
        var resolvedUntil = until ?? today;

        var result = await getService.ExecuteAsync(tenantId, resolvedSince, resolvedUntil, ct);
        return Ok(result);
    }

    [HttpPost("sync")]
    [ProducesResponseType(typeof(DailySalesSyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Sync([FromBody] DailySalesSyncRequest request, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await syncService.ExecuteAsync(tenantId, request.Year, request.Month, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
