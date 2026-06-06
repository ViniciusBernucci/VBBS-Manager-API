using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.CashFlow.UpdateTransaction;

public interface IUpdateTransactionService
{
    Task<Result> ExecuteAsync(Guid tenantId, Guid transactionId, UpdateTransactionRequest request, CancellationToken ct);
}

public class UpdateTransactionService(AppDbContext db) : IUpdateTransactionService
{
    public async Task<Result> ExecuteAsync(Guid tenantId, Guid transactionId, UpdateTransactionRequest request, CancellationToken ct)
    {
        var transaction = await db.CashFlowTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.TenantId == tenantId, ct);

        if (transaction is null)
            return Result.Fail("Transaction not found.");

        transaction.Date = request.Date;
        transaction.Description = request.Description;
        transaction.Amount = request.Amount;
        transaction.Type = request.Type;
        transaction.Category = request.Category;
        transaction.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
