using VBBSManager.Domain.Enums;

namespace VBBSManager.Api.Features.Financial.FixedExpenses.Save;

public record SaveFixedExpenseRequest(
    string Name,
    decimal Amount,
    CashFlowCategory Category
);
