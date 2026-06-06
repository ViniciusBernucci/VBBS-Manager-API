using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Domain.Enums;
using VBBSManager.Infrastructure.Persistence;
using CashFlowTransactionEntity = VBBSManager.Domain.Entities.CashFlowTransaction;

namespace VBBSManager.Api.Features.Financial.FixedExpenses.MonthlyStatus;

public interface IGetMonthlyStatusService
{
    Task<Result<List<MonthlyFixedExpenseDto>>> ExecuteAsync(Guid tenantId, int year, int month, CancellationToken ct);
}

public class GetMonthlyStatusService(AppDbContext db) : IGetMonthlyStatusService
{
    public async Task<Result<List<MonthlyFixedExpenseDto>>> ExecuteAsync(Guid tenantId, int year, int month, CancellationToken ct)
    {
        var expenses = await db.FixedExpenses
            .Where(f => f.TenantId == tenantId && f.IsActive)
            .OrderBy(f => f.Category).ThenBy(f => f.Name)
            .ToListAsync(ct);

        var payments = await db.FixedExpensePayments
            .Where(p => p.TenantId == tenantId && p.Year == year && p.Month == month)
            .ToListAsync(ct);

        var transactionIds = payments
            .Where(p => p.CashFlowTransactionId.HasValue)
            .Select(p => p.CashFlowTransactionId!.Value)
            .ToList();

        var transactions = transactionIds.Any()
            ? await db.CashFlowTransactions
                .Where(t => transactionIds.Contains(t.Id))
                .ToListAsync(ct)
            : [];

        var txLookup = transactions.ToDictionary(t => t.Id);
        var paymentLookup = payments.ToDictionary(p => p.FixedExpenseId);

        var dtos = expenses.Select(f =>
        {
            var hasPay = paymentLookup.TryGetValue(f.Id, out var payment);
            CashFlowTransactionEntity? tx = null;
            if (hasPay && payment!.CashFlowTransactionId.HasValue)
                txLookup.TryGetValue(payment.CashFlowTransactionId.Value, out tx);

            return new MonthlyFixedExpenseDto(
                f.Id,
                f.Name,
                f.Amount,
                f.Category.ToString(),
                GetCategoryLabel(f.Category),
                hasPay,
                hasPay ? payment!.Id : null,
                tx?.Amount,
                tx?.Date
            );
        }).ToList();

        return Result<List<MonthlyFixedExpenseDto>>.Ok(dtos);
    }

    private static string GetCategoryLabel(CashFlowCategory category) => category switch
    {
        CashFlowCategory.MetaAds      => "Meta Ads",
        CashFlowCategory.Taxes        => "Impostos",
        CashFlowCategory.Tools        => "Ferramentas / SaaS",
        CashFlowCategory.OtherExpense => "Outras Saídas",
        _ => category.ToString()
    };
}
