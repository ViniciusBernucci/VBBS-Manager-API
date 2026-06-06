using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Domain.Entities;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.CashFlow.CreateTransaction;

public interface ICreateTransactionService
{
    Task<Result<Guid>> ExecuteAsync(Guid tenantId, CreateTransactionRequest request, CancellationToken ct);
}

public class CreateTransactionService(AppDbContext db) : ICreateTransactionService
{
    public async Task<Result<Guid>> ExecuteAsync(Guid tenantId, CreateTransactionRequest request, CancellationToken ct)
    {
        var transaction = new CashFlowTransaction
        {
            TenantId = tenantId,
            Date = request.Date,
            Description = request.Description,
            Amount = request.Amount,
            Type = request.Type,
            Category = request.Category
        };

        db.CashFlowTransactions.Add(transaction);

        if (request.FixedExpenseId.HasValue)
        {
            var year  = request.Date.Year;
            var month = request.Date.Month;

            var alreadyPaid = await db.FixedExpensePayments.AnyAsync(
                p => p.TenantId == tenantId
                  && p.FixedExpenseId == request.FixedExpenseId.Value
                  && p.Year == year && p.Month == month, ct);

            if (!alreadyPaid)
            {
                db.FixedExpensePayments.Add(new FixedExpensePayment
                {
                    TenantId = tenantId,
                    FixedExpenseId = request.FixedExpenseId.Value,
                    Year = year,
                    Month = month,
                    CashFlowTransactionId = transaction.Id
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(transaction.Id);
    }
}
