using VBBSManager.Api.Common.Results;

namespace VBBSManager.Api.Features.Financial.DRE;

public interface IDreService
{
    Task<Result<DreResponse>> ExecuteAsync(Guid tenantId, int year, int month, CancellationToken ct);
}

public class DreService(ILogger<DreService> logger) : IDreService
{
    public async Task<Result<DreResponse>> ExecuteAsync(Guid tenantId, int year, int month, CancellationToken ct)
    {
        // TODO: agregar receitas Hotmart e gastos Meta Ads do mês
        // TODO: calcular projeção de fechamento com base no ritmo atual
        logger.LogInformation("DRE for tenant {TenantId} {Year}/{Month}", tenantId, year, month);

        await Task.CompletedTask;
        return Result<DreResponse>.Fail("Not implemented");
    }
}
