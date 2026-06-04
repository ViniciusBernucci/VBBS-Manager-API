using Microsoft.AspNetCore.Mvc;
using VBBSManager.Api.Features.Auth.Login;

namespace VBBSManager.Api.Features.Auth.RefreshToken;

[ApiController]
[Route("api/auth")]
public class RefreshTokenController(IRefreshTokenService service) : ControllerBase
{
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await service.ExecuteAsync(request, ct);

        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Value);
    }
}
