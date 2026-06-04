using VBBSManager.Api.Common.Results;

namespace VBBSManager.Api.Features.Auth.Login;

public interface ILoginService
{
    Task<Result<LoginResponse>> ExecuteAsync(LoginRequest request, CancellationToken ct);
}

public class LoginService(ILogger<LoginService> logger) : ILoginService
{
    public async Task<Result<LoginResponse>> ExecuteAsync(LoginRequest request, CancellationToken ct)
    {
        // TODO: validar credenciais contra banco via EF Core
        // TODO: gerar JWT + RefreshToken
        logger.LogInformation("Login attempt for {Email}", request.Email);

        await Task.CompletedTask;
        return Result<LoginResponse>.Fail("Not implemented");
    }
}
