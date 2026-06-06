using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Financial.FixedExpenses.Save;

[ApiController]
[Route("api/financial/fixed-expenses")]
[Authorize]
public class SaveFixedExpenseController(ISaveFixedExpenseService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] SaveFixedExpenseRequest request, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.CreateAsync(tenantId, request, ct);
        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveFixedExpenseRequest request, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.UpdateAsync(tenantId, id, request, ct);
        if (!result.IsSuccess)
            return result.Error!.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { error = result.Error })
                : StatusCode(500, new { error = result.Error });
        return NoContent();
    }

    [HttpPatch("{id:guid}/toggle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ToggleActiveAsync(tenantId, id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.DeleteAsync(tenantId, id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });
        return NoContent();
    }
}
