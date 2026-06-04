using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Auth.Login;

[ApiController]
[Route("api/auth")]
public class LoginController(ILoginService service) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await service.ExecuteAsync(request, ct);

        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Value);
    }
}
