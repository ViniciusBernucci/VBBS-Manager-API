namespace VBBSManager.Infrastructure.ExternalClients.Meta;

public class MetaApiException(int code, string message) : Exception(message)
{
    public int Code { get; } = code;
}

public class MetaTokenException(string message) : MetaApiException(190, message);

public class MetaPermissionException(string message) : MetaApiException(200, message);

public class MetaRateLimitException(string message) : MetaApiException(17, message);
