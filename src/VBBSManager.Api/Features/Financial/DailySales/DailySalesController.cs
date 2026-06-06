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
    [ProducesResponseType(typeof(DailySalesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetToday(CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await getService.ExecuteAsync(tenantId, ct);
        return Ok(result); // null serializa como JSON null → 200 com body null
    }

    [HttpPost("sync")]
    [ProducesResponseType(typeof(DailySalesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Sync([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await syncService.ExecuteAsync(tenantId, date, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
