using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Financial.FixedExpenses.Pay;

[ApiController]
[Route("api/financial/fixed-expenses")]
[Authorize]
public class PayFixedExpenseController(IPayFixedExpenseService service) : ControllerBase
{
    [HttpPost("{id:guid}/pay")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Pay(Guid id, [FromBody] PayFixedExpenseRequest request, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.PayAsync(tenantId, id, request, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { paymentId = result.Value });
    }

    [HttpDelete("payments/{paymentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unpay(Guid paymentId, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.UnpayAsync(tenantId, paymentId, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });
        return NoContent();
    }
}
