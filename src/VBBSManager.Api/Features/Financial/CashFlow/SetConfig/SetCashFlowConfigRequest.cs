namespace VBBSManager.Api.Features.Financial.CashFlow.SetConfig;

public record SetCashFlowConfigRequest(
    int InitialYear,
    int InitialMonth,
    decimal InitialBalance
);
