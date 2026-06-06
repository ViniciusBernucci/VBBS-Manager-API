namespace VBBSManager.Api.Features.Financial.FixedExpenses.List;

public record FixedExpenseDto(
    Guid Id,
    string Name,
    decimal Amount,
    string Category,
    string CategoryLabel,
    bool IsActive
);
