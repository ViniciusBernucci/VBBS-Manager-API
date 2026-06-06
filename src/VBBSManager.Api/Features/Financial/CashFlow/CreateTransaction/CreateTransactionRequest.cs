using VBBSManager.Domain.Enums;

namespace VBBSManager.Api.Features.Financial.CashFlow.CreateTransaction;

public record CreateTransactionRequest(
    DateOnly Date,
    string Description,
    decimal Amount,
    TransactionType Type,
    CashFlowCategory Category,
    Guid? FixedExpenseId = null
);
