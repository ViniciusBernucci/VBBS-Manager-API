using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VBBSManager.Api.Common.Results;
using VBBSManager.Infrastructure.Persistence;
using RefreshTokenEntity = VBBSManager.Domain.Entities.RefreshToken;
using UserEntity = VBBSManager.Domain.Entities.User;

namespace VBBSManager.Api.Features.Auth.Login;

public interface ILoginService
{
    Task<Result<LoginResponse>> ExecuteAsync(LoginRequest request, CancellationToken ct);
}

public class LoginService(AppDbContext db, IConfiguration config, ILogger<LoginService> logger)
    : ILoginService
{
    public async Task<Result<LoginResponse>> ExecuteAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, ct);

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Result<LoginResponse>.Fail("E-mail ou senha inválidos.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(
            config.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15));

        var accessToken = GenerateJwt(user, expiresAt);
        var refreshToken = await CreateRefreshToken(user, ct);

        logger.LogInformation("Successful login for {Email} (tenant {TenantId})", user.Email, user.TenantId);

        return Result<LoginResponse>.Ok(new LoginResponse(
            accessToken,
            refreshToken.Token,
            expiresAt,
            user.Name,
            user.TenantId
        ));
    }

    private string GenerateJwt(UserEntity user, DateTime expiresAt)
    {
        var secret = config["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("tenant_id", user.TenantId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshTokenEntity> CreateRefreshToken(UserEntity user, CancellationToken ct)
    {
        var days = config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
        var token = new RefreshTokenEntity
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(days)
        };

        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
        return token;
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[0]);
            expectedHash = Convert.FromBase64String(parts[1]);
        }
        catch { return false; }

        var actualHash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100_000,
            numBytesRequested: 32
        );

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
