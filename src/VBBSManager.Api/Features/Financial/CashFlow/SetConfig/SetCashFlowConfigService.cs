using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Domain.Entities;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.CashFlow.SetConfig;

public interface ISetCashFlowConfigService
{
    Task<Result> ExecuteAsync(Guid tenantId, SetCashFlowConfigRequest request, CancellationToken ct);
}

public class SetCashFlowConfigService(AppDbContext db, ILogger<SetCashFlowConfigService> logger)
    : ISetCashFlowConfigService
{
    public async Task<Result> ExecuteAsync(Guid tenantId, SetCashFlowConfigRequest request, CancellationToken ct)
    {
        var config = await db.CashFlowConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (config is null)
        {
            config = new CashFlowConfig { TenantId = tenantId };
            db.CashFlowConfigs.Add(config);
        }

        config.InitialYear = request.InitialYear;
        config.InitialMonth = request.InitialMonth;
        config.InitialBalance = request.InitialBalance;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Set cash flow config for tenant {TenantId}: {Year}/{Month}, balance {Balance}",
            tenantId, request.InitialYear, request.InitialMonth, request.InitialBalance);

        return Result.Ok();
    }
}
