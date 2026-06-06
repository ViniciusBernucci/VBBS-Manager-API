using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.CashFlow.DeleteTransaction;

public interface IDeleteTransactionService
{
    Task<Result> ExecuteAsync(Guid tenantId, Guid transactionId, CancellationToken ct);
}

public class DeleteTransactionService(AppDbContext db) : IDeleteTransactionService
{
    public async Task<Result> ExecuteAsync(Guid tenantId, Guid transactionId, CancellationToken ct)
    {
        var transaction = await db.CashFlowTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.TenantId == tenantId, ct);

        if (transaction is null)
            return Result.Fail("Transaction not found.");

        db.CashFlowTransactions.Remove(transaction);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
