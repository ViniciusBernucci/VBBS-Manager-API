using VBBSManager.Domain.Enums;

namespace VBBSManager.Domain.Entities;

public class Alert : BaseEntity
{
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
    public string? Metadata { get; set; }
}
