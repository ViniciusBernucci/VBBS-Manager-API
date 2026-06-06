using VBBSManager.Domain.Enums;

namespace VBBSManager.Api.Features.Financial.CashFlow.UpdateTransaction;

public record UpdateTransactionRequest(
    DateOnly Date,
    string Description,
    decimal Amount,
    TransactionType Type,
    CashFlowCategory Category
);
