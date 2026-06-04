using VBBSManager.Api.Common.Results;

namespace VBBSManager.Api.Features.Financial.Overview;

public interface IFinancialOverviewService
{
    Task<Result<FinancialOverviewResponse>> ExecuteAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct);
}

public class FinancialOverviewService(ILogger<FinancialOverviewService> logger) : IFinancialOverviewService
{
    public async Task<Result<FinancialOverviewResponse>> ExecuteAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        // TODO: buscar vendas Hotmart + gasto Meta Ads do banco para o período
        // TODO: calcular KPIs e variação vs. período anterior equivalente
        logger.LogInformation("Financial overview for tenant {TenantId} from {From} to {To}", tenantId, from, to);

        await Task.CompletedTask;
        return Result<FinancialOverviewResponse>.Fail("Not implemented");
    }
}
