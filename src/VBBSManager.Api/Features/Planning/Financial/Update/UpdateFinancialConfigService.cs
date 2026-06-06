using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Planning.Financial.Update;

public interface IUpdateFinancialConfigService
{
    Task<Result> ExecuteAsync(Guid tenantId, UpdateFinancialConfigRequest request, CancellationToken ct);
}

public class UpdateFinancialConfigService(AppDbContext db, ILogger<UpdateFinancialConfigService> logger)
    : IUpdateFinancialConfigService
{
    public async Task<Result> ExecuteAsync(Guid tenantId, UpdateFinancialConfigRequest request, CancellationToken ct)
    {
        var config = await db.FinancialConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (config is null)
            return Result.Fail("Financial config not found. Call GET first to initialize defaults.");

        config.MonthlyGrossRevenue = request.MonthlyGrossRevenue;
        config.MonthlyAdSpend = request.MonthlyAdSpend;
        config.HotmartFeePercent = request.HotmartFeePercent;
        config.InstallmentFeePercent = request.InstallmentFeePercent;
        config.InstallmentSalesPercent = request.InstallmentSalesPercent;
        config.FederalTaxPercent = request.FederalTaxPercent;
        config.RefundRatePercent = request.RefundRatePercent;
        config.MetaAdsTaxPercent = request.MetaAdsTaxPercent;
        config.AccountingCost = request.AccountingCost;
        config.InvoicingCost = request.InvoicingCost;
        config.ManychatCost = request.ManychatCost;
        config.HotmartPlayerCost = request.HotmartPlayerCost;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Updated financial config for tenant {TenantId}", tenantId);

        return Result.Ok();
    }
}
