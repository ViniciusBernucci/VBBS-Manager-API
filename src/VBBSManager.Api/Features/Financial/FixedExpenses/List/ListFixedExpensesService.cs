using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Domain.Enums;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.FixedExpenses.List;

public interface IListFixedExpensesService
{
    Task<Result<List<FixedExpenseDto>>> ExecuteAsync(Guid tenantId, CancellationToken ct);
}

public class ListFixedExpensesService(AppDbContext db) : IListFixedExpensesService
{
    public async Task<Result<List<FixedExpenseDto>>> ExecuteAsync(Guid tenantId, CancellationToken ct)
    {
        var expenses = await db.FixedExpenses
            .Where(f => f.TenantId == tenantId)
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Name)
            .ToListAsync(ct);

        var dtos = expenses.Select(f => new FixedExpenseDto(
            f.Id,
            f.Name,
            f.Amount,
            f.Category.ToString(),
            GetCategoryLabel(f.Category),
            f.IsActive
        )).ToList();

        return Result<List<FixedExpenseDto>>.Ok(dtos);
    }

    internal static string GetCategoryLabel(CashFlowCategory category) => category switch
    {
        CashFlowCategory.MetaAds      => "Meta Ads",
        CashFlowCategory.Taxes        => "Impostos",
        CashFlowCategory.Tools        => "Ferramentas / SaaS",
        CashFlowCategory.OtherExpense => "Outras Saídas",
        _ => category.ToString()
    };
}
