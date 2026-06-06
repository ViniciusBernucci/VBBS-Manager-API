using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Domain.Entities;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.FixedExpenses.Save;

public interface ISaveFixedExpenseService
{
    Task<Result<Guid>> CreateAsync(Guid tenantId, SaveFixedExpenseRequest request, CancellationToken ct);
    Task<Result> UpdateAsync(Guid tenantId, Guid id, SaveFixedExpenseRequest request, CancellationToken ct);
    Task<Result> ToggleActiveAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<Result> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct);
}

public class SaveFixedExpenseService(AppDbContext db) : ISaveFixedExpenseService
{
    public async Task<Result<Guid>> CreateAsync(Guid tenantId, SaveFixedExpenseRequest request, CancellationToken ct)
    {
        var expense = new FixedExpense
        {
            TenantId = tenantId,
            Name = request.Name,
            Amount = request.Amount,
            Category = request.Category,
            IsActive = true
        };

        db.FixedExpenses.Add(expense);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(expense.Id);
    }

    public async Task<Result> UpdateAsync(Guid tenantId, Guid id, SaveFixedExpenseRequest request, CancellationToken ct)
    {
        var expense = await db.FixedExpenses
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId, ct);

        if (expense is null)
            return Result.Fail("Gasto fixo não encontrado.");

        expense.Name = request.Name;
        expense.Amount = request.Amount;
        expense.Category = request.Category;
        expense.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> ToggleActiveAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var expense = await db.FixedExpenses
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId, ct);

        if (expense is null)
            return Result.Fail("Gasto fixo não encontrado.");

        expense.IsActive = !expense.IsActive;
        expense.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var expense = await db.FixedExpenses
            .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId, ct);

        if (expense is null)
            return Result.Fail("Gasto fixo não encontrado.");

        db.FixedExpenses.Remove(expense);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
