using VBBSManager.Api.Common.Results;

namespace VBBSManager.Api.Features.Creatives.List;

public interface ICreativesListService
{
    Task<Result<CreativesListResponse>> ExecuteAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct);
}

public class CreativesListService(ILogger<CreativesListService> logger) : ICreativesListService
{
    public async Task<Result<CreativesListResponse>> ExecuteAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        // TODO: buscar métricas de criativos sincronizadas do Meta Ads
        // TODO: aplicar semáforo com base nos thresholds do tenant
        logger.LogInformation("Creatives list for tenant {TenantId}", tenantId);

        await Task.CompletedTask;
        return Result<CreativesListResponse>.Fail("Not implemented");
    }
}
