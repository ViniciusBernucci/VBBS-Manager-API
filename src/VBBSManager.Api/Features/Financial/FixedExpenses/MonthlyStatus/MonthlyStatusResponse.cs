namespace VBBSManager.Api.Features.Financial.FixedExpenses.MonthlyStatus;

public record MonthlyFixedExpenseDto(
    Guid FixedExpenseId,
    string Name,
    decimal BudgetedAmount,
    string Category,
    string CategoryLabel,
    bool IsPaid,
    Guid? PaymentId,
    decimal? PaidAmount,
    DateOnly? PaidDate
);
