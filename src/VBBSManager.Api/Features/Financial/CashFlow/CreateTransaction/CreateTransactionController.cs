using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Financial.CashFlow.CreateTransaction;

[ApiController]
[Route("api/financial/cash-flow")]
[Authorize]
public class CreateTransactionController(ICreateTransactionService service) : ControllerBase
{
    [HttpPost("transactions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, request, ct);
        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.Error });
        return Ok(new { id = result.Value });
    }
}
