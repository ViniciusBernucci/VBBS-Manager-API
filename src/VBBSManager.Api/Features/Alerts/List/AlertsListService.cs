using VBBSManager.Api.Common.Results;

namespace VBBSManager.Api.Features.Alerts.List;

public interface IAlertsListService
{
    Task<Result<AlertsListResponse>> ExecuteAsync(Guid tenantId, bool onlyUnread, CancellationToken ct);
}

public class AlertsListService(ILogger<AlertsListService> logger) : IAlertsListService
{
    public async Task<Result<AlertsListResponse>> ExecuteAsync(Guid tenantId, bool onlyUnread, CancellationToken ct)
    {
        // TODO: buscar alertas do banco filtrando por tenant_id e status
        logger.LogInformation("Alerts list for tenant {TenantId}", tenantId);

        await Task.CompletedTask;
        return Result<AlertsListResponse>.Fail("Not implemented");
    }
}
