using System.Security.Claims;

namespace VBBSManager.Api.Common.Middleware;

public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantClaim = context.User.FindFirstValue("tenant_id");

        if (tenantClaim is not null && Guid.TryParse(tenantClaim, out var tenantId))
        {
            context.Items["TenantId"] = tenantId;
        }

        await next(context);
    }
}
