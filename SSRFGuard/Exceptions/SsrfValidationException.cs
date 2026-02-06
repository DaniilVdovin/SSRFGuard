namespace SSRFGuard.Exceptions;

public class SsrfValidationException : Exception
{
    public string BlockedUrl { get; }

    public SsrfValidationException(string url, string reason)
        : base($"SSRF validation failed: {reason}")
    {
        BlockedUrl = url;
    }
}
