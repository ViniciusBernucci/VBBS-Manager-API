namespace VBBSManager.Api.Features.Financial.CashFlow.GetCashFlow;

public record CashFlowResponse(
    int Year,
    int Month,
    bool IsConfigured,
    decimal OpeningBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal ClosingBalance,
    List<CashFlowTransactionDto> Transactions,
    CashFlowConfigDto? Config
);

public record CashFlowConfigDto(
    int InitialYear,
    int InitialMonth,
    decimal InitialBalance
);

public record CashFlowTransactionDto(
    Guid Id,
    DateOnly Date,
    string Description,
    decimal Amount,
    string Type,
    string Category,
    string CategoryLabel,
    bool IsFixed,
    bool IsPaid,
    Guid? PaymentId
);
