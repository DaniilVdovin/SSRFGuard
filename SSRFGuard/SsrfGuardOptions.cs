namespace SSRFGuard;

public class SsrfGuardOptions
{
    public bool Enabled { get; set; } = true;
    
    public HashSet<string> AllowedSchemes { get; set; } = new() { "http", "https" };
    
    public HashSet<string> AllowedDomains { get; set; } = new();
    
    public TimeSpan? Timeout { get; set; }
}
