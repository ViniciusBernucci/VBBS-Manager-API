using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Domain.Entities;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Planning.Financial.Get;

public interface IGetFinancialConfigService
{
    Task<Result<FinancialConfigResponse>> ExecuteAsync(Guid tenantId, CancellationToken ct);
}

public class GetFinancialConfigService(AppDbContext db, ILogger<GetFinancialConfigService> logger)
    : IGetFinancialConfigService
{
    public async Task<Result<FinancialConfigResponse>> ExecuteAsync(Guid tenantId, CancellationToken ct)
    {
        var config = await db.FinancialConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (config is null)
        {
            config = BuildDefault(tenantId);
            db.FinancialConfigs.Add(config);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded default financial config for tenant {TenantId}", tenantId);
        }

        return Result<FinancialConfigResponse>.Ok(new FinancialConfigResponse(
            config.Id,
            config.MonthlyGrossRevenue,
            config.MonthlyAdSpend,
            config.HotmartFeePercent,
            config.HotmartFixedFeePerTransaction,
            config.InstallmentFeePercent,
            config.InstallmentSalesPercent,
            config.FederalTaxPercent,
            config.RefundRatePercent,
            config.MetaAdsTaxPercent,
            config.AccountingCost,
            config.InvoicingCost,
            config.ManychatCost,
            config.HotmartPlayerCost
        ));
    }

    private static FinancialConfig BuildDefault(Guid tenantId) => new()
    {
        TenantId = tenantId,
        MonthlyGrossRevenue = 12370m,
        MonthlyAdSpend = 8000m,
        HotmartFeePercent = 0.084m,
        HotmartFixedFeePerTransaction = 0.54m,
        InstallmentFeePercent = 0.0219m,
        InstallmentSalesPercent = 0.33m,
        FederalTaxPercent = 0.06m,
        RefundRatePercent = 0.01m,
        MetaAdsTaxPercent = 0.10m,
        AccountingCost = 400m,
        InvoicingCost = 250m,
        ManychatCost = 448m,
        HotmartPlayerCost = 69m,
    };
}
