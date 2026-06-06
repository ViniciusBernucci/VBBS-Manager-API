namespace VBBSManager.Api.Features.Financial.FixedExpenses.Pay;

public record PayFixedExpenseRequest(
    int Year,
    int Month,
    decimal Amount,
    DateOnly Date
);
