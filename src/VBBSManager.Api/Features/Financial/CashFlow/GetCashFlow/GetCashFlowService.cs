using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Domain.Enums;
using VBBSManager.Infrastructure.Persistence;
using CashFlowTransactionEntity = VBBSManager.Domain.Entities.CashFlowTransaction;

namespace VBBSManager.Api.Features.Financial.CashFlow.GetCashFlow;

public interface IGetCashFlowService
{
    Task<Result<CashFlowResponse>> ExecuteAsync(Guid tenantId, int year, int month, CancellationToken ct);
}

public class GetCashFlowService(AppDbContext db) : IGetCashFlowService
{
    public async Task<Result<CashFlowResponse>> ExecuteAsync(Guid tenantId, int year, int month, CancellationToken ct)
    {
        var config = await db.CashFlowConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (config is null)
            return Result<CashFlowResponse>.Ok(new CashFlowResponse(year, month, false, 0, 0, 0, 0, [], null));

        var startDate    = new DateOnly(config.InitialYear, config.InitialMonth, 1);
        var requestedDate = new DateOnly(year, month, 1);
        var monthStart   = requestedDate;
        var monthEnd     = monthStart.AddMonths(1);

        // ── payments for the current month ───────────────────────────────────────
        var monthPayments = await db.FixedExpensePayments
            .Where(p => p.TenantId == tenantId && p.Year == year && p.Month == month)
            .ToListAsync(ct);

        var paidTxIds = monthPayments
            .Where(p => p.CashFlowTransactionId.HasValue)
            .Select(p => p.CashFlowTransactionId!.Value)
            .ToHashSet();

        // ── opening balance ───────────────────────────────────────────────────────
        decimal openingBalance;

        if (requestedDate <= startDate)
        {
            openingBalance = config.InitialBalance;
        }
        else
        {
            var priorTx = await db.CashFlowTransactions
                .Where(t => t.TenantId == tenantId && t.Date >= startDate && t.Date < requestedDate)
                .ToListAsync(ct);

            var priorIncome  = priorTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var priorExpense = priorTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

            // Add budgeted amount for fixed expenses that were NOT paid in prior months
            var priorPaymentsByMonth = await db.FixedExpensePayments
                .Where(p => p.TenantId == tenantId
                    && ((p.Year == startDate.Year && p.Month >= startDate.Month) || p.Year > startDate.Year)
                    && ((p.Year == year && p.Month < month) || p.Year < year))
                .ToListAsync(ct);

            var activeFixed = await db.FixedExpenses
                .Where(f => f.TenantId == tenantId && f.IsActive)
                .ToListAsync(ct);

            var priorMonths = MonthsBetween(startDate, requestedDate);
            // For each prior month, count budgeted expense for unpaid fixed expenses
            // (paid ones are already counted via real transactions)
            var paidFixedExpenseMonths = priorPaymentsByMonth
                .GroupBy(p => p.FixedExpenseId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var f in activeFixed)
            {
                var paidMonths = paidFixedExpenseMonths.GetValueOrDefault(f.Id, 0);
                var unpaidMonths = priorMonths - paidMonths;
                if (unpaidMonths > 0)
                    priorExpense += f.Amount * unpaidMonths;
            }

            openingBalance = config.InitialBalance + priorIncome - priorExpense;
        }

        // ── manual transactions (excluding those linked to fixed expense payments) ─
        var manualTx = await db.CashFlowTransactions
            .Where(t => t.TenantId == tenantId && t.Date >= monthStart && t.Date < monthEnd
                        && !paidTxIds.Contains(t.Id))
            .OrderBy(t => t.Date).ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

        // ── paid transactions linked to fixed expenses ───────────────────────────
        var paidTxList = paidTxIds.Any()
            ? await db.CashFlowTransactions.Where(t => paidTxIds.Contains(t.Id)).ToListAsync(ct)
            : [];
        var paidTxById = paidTxList.ToDictionary(t => t.Id);

        // ── active fixed expenses ────────────────────────────────────────────────
        var fixedExpenses = await db.FixedExpenses
            .Where(f => f.TenantId == tenantId && f.IsActive)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);

        var paymentByExpense = monthPayments.ToDictionary(p => p.FixedExpenseId);

        // ── totals ───────────────────────────────────────────────────────────────
        var totalIncome  = manualTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount)
                         + paidTxList.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);

        var totalExpense = manualTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
                         + fixedExpenses.Sum(f =>
                         {
                             if (paymentByExpense.TryGetValue(f.Id, out var pay) && pay.CashFlowTransactionId.HasValue)
                                 return paidTxById.TryGetValue(pay.CashFlowTransactionId.Value, out var tx) ? tx.Amount : f.Amount;
                             return f.Amount;
                         });

        var closingBalance = openingBalance + totalIncome - totalExpense;

        // ── DTOs ─────────────────────────────────────────────────────────────────
        var dtos = manualTx.Select(t => new CashFlowTransactionDto(
            t.Id, t.Date, t.Description, t.Amount,
            t.Type.ToString(), t.Category.ToString(), GetCategoryLabel(t.Category),
            false, true, null
        )).ToList();

        var fixedDtos = fixedExpenses.Select(f =>
        {
            var hasPay = paymentByExpense.TryGetValue(f.Id, out var pay);
            CashFlowTransactionEntity? tx = null;
            if (hasPay && pay!.CashFlowTransactionId.HasValue)
                paidTxById.TryGetValue(pay.CashFlowTransactionId.Value, out tx);

            return new CashFlowTransactionDto(
                f.Id,
                tx?.Date ?? monthStart,
                f.Name,
                tx?.Amount ?? f.Amount,
                "Expense",
                f.Category.ToString(),
                GetCategoryLabel(f.Category),
                true,
                hasPay,
                hasPay ? pay!.Id : null
            );
        }).ToList();

        dtos.InsertRange(0, fixedDtos);

        return Result<CashFlowResponse>.Ok(new CashFlowResponse(
            year, month, true,
            openingBalance, totalIncome, totalExpense, closingBalance,
            dtos,
            new CashFlowConfigDto(config.InitialYear, config.InitialMonth, config.InitialBalance)
        ));
    }

    private static int MonthsBetween(DateOnly from, DateOnly to)
        => (to.Year - from.Year) * 12 + (to.Month - from.Month);

    private static string GetCategoryLabel(CashFlowCategory category) => category switch
    {
        CashFlowCategory.HotmartPix   => "Vendas Hotmart (Pix)",
        CashFlowCategory.HotmartCard  => "Vendas Hotmart (Cartão)",
        CashFlowCategory.OtherIncome  => "Outras Entradas",
        CashFlowCategory.MetaAds      => "Meta Ads",
        CashFlowCategory.Taxes        => "Impostos",
        CashFlowCategory.Tools        => "Ferramentas / SaaS",
        CashFlowCategory.OtherExpense => "Outras Saídas",
        _ => category.ToString()
    };
}
