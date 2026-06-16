namespace VBBSManager.Infrastructure.ExternalClients.Meta;

public class MetaSettings
{
    public string AccessToken { get; set; } = string.Empty;

    private string _adAccountId = string.Empty;

    // Garante o prefixo act_ independente do formato configurado no .env
    public string AdAccountId
    {
        get => _adAccountId;
        set => _adAccountId = value.StartsWith("act_", StringComparison.OrdinalIgnoreCase)
            ? value
            : $"act_{value}";
    }
}
