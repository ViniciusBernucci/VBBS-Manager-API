using VBBSManager.Api.Common.Results;

namespace VBBSManager.Api.Features.Alerts.MarkRead;

public interface IMarkAlertReadService
{
    Task<Result> ExecuteAsync(Guid tenantId, Guid alertId, bool resolved, CancellationToken ct);
}

public class MarkAlertReadService(ILogger<MarkAlertReadService> logger) : IMarkAlertReadService
{
    public async Task<Result> ExecuteAsync(Guid tenantId, Guid alertId, bool resolved, CancellationToken ct)
    {
        // TODO: atualizar alerta garantindo que pertence ao tenant_id
        logger.LogInformation("Marking alert {AlertId} as read for tenant {TenantId}", alertId, tenantId);

        await Task.CompletedTask;
        return Result.Fail("Not implemented");
    }
}
