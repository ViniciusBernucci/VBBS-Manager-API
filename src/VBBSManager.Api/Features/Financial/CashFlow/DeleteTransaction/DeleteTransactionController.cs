using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Financial.CashFlow.DeleteTransaction;

[ApiController]
[Route("api/financial/cash-flow")]
[Authorize]
public class DeleteTransactionController(IDeleteTransactionService service) : ControllerBase
{
    [HttpDelete("transactions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, id, ct);
        if (!result.IsSuccess)
            return result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { error = result.Error })
                : StatusCode(500, new { error = result.Error });
        return NoContent();
    }
}
