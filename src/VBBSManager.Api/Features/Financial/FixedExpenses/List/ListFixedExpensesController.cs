using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Financial.FixedExpenses.List;

[ApiController]
[Route("api/financial/fixed-expenses")]
[Authorize]
public class ListFixedExpensesController(IListFixedExpensesService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<FixedExpenseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, ct);
        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.Error });
        return Ok(result.Value);
    }
}
