using VBBSManager.Api.Common.Results;
using VBBSManager.Api.Features.Auth.Login;

namespace VBBSManager.Api.Features.Auth.RefreshToken;

public interface IRefreshTokenService
{
    Task<Result<LoginResponse>> ExecuteAsync(RefreshTokenRequest request, CancellationToken ct);
}

public class RefreshTokenService(ILogger<RefreshTokenService> logger) : IRefreshTokenService
{
    public async Task<Result<LoginResponse>> ExecuteAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        // TODO: validar refresh token no banco, verificar expiração e revogação
        // TODO: revogar token antigo, emitir novo par JWT + RefreshToken
        logger.LogInformation("Refresh token attempt");

        await Task.CompletedTask;
        return Result<LoginResponse>.Fail("Not implemented");
    }
}
