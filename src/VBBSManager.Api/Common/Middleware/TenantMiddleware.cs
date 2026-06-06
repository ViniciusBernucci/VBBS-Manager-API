using System.Security.Claims;

namespace VBBSManager.Api.Common.Middleware;

public class TenantMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    // Tenant fixo para desenvolvimento local — nunca usar em produção
    private static readonly Guid DevTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantClaim = context.User.FindFirstValue("tenant_id");

        if (tenantClaim is not null && Guid.TryParse(tenantClaim, out var tenantId))
        {
            context.Items["TenantId"] = tenantId;
        }
        else if (env.IsDevelopment())
        {
            context.Items["TenantId"] = DevTenantId;
        }

        await next(context);
    }
}
