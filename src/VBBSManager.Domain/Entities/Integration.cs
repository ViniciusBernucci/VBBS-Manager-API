using VBBSManager.Domain.Enums;

namespace VBBSManager.Domain.Entities;

public class Integration : BaseEntity
{
    public IntegrationProvider Provider { get; set; }
    public string CredentialsEncrypted { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncAt { get; set; }
}
