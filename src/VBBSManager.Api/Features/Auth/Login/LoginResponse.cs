namespace VBBSManager.Api.Features.Auth.Login;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserName,
    Guid TenantId
);
