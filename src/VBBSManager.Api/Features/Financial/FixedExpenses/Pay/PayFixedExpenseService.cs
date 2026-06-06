using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Domain.Entities;
using VBBSManager.Domain.Enums;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.FixedExpenses.Pay;

public interface IPayFixedExpenseService
{
    Task<Result<Guid>> PayAsync(Guid tenantId, Guid fixedExpenseId, PayFixedExpenseRequest request, CancellationToken ct);
    Task<Result> UnpayAsync(Guid tenantId, Guid paymentId, CancellationToken ct);
}

public class PayFixedExpenseService(AppDbContext db) : IPayFixedExpenseService
{
    public async Task<Result<Guid>> PayAsync(Guid tenantId, Guid fixedExpenseId, PayFixedExpenseRequest request, CancellationToken ct)
    {
        var expense = await db.FixedExpenses
            .FirstOrDefaultAsync(f => f.Id == fixedExpenseId && f.TenantId == tenantId, ct);

        if (expense is null)
            return Result<Guid>.Fail("Gasto fixo não encontrado.");

        var existing = await db.FixedExpensePayments
            .FirstOrDefaultAsync(p => p.TenantId == tenantId
                && p.FixedExpenseId == fixedExpenseId
                && p.Year == request.Year
                && p.Month == request.Month, ct);

        if (existing is not null)
            return Result<Guid>.Fail("Este gasto já foi marcado como pago neste mês.");

        var transaction = new CashFlowTransaction
        {
            TenantId = tenantId,
            Date = request.Date,
            Description = expense.Name,
            Amount = request.Amount,
            Type = TransactionType.Expense,
            Category = expense.Category
        };

        db.CashFlowTransactions.Add(transaction);

        var payment = new FixedExpensePayment
        {
            TenantId = tenantId,
            FixedExpenseId = fixedExpenseId,
            Year = request.Year,
            Month = request.Month,
            CashFlowTransactionId = transaction.Id
        };

        db.FixedExpensePayments.Add(payment);
        await db.SaveChangesAsync(ct);

        return Result<Guid>.Ok(payment.Id);
    }

    public async Task<Result> UnpayAsync(Guid tenantId, Guid paymentId, CancellationToken ct)
    {
        var payment = await db.FixedExpensePayments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.TenantId == tenantId, ct);

        if (payment is null)
            return Result.Fail("Pagamento não encontrado.");

        if (payment.CashFlowTransactionId.HasValue)
        {
            var transaction = await db.CashFlowTransactions
                .FirstOrDefaultAsync(t => t.Id == payment.CashFlowTransactionId, ct);
            if (transaction is not null)
                db.CashFlowTransactions.Remove(transaction);
        }

        db.FixedExpensePayments.Remove(payment);
        await db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
