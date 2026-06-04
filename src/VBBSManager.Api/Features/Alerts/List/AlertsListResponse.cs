using VBBSManager.Domain.Enums;

namespace VBBSManager.Api.Features.Alerts.List;

public record AlertsListResponse(IReadOnlyList<AlertItem> Items, int TotalUnread);

public record AlertItem(
    Guid Id,
    AlertType Type,
    AlertSeverity Severity,
    string Title,
    string Message,
    bool IsRead,
    bool IsResolved,
    DateTime CreatedAt
);
