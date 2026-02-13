// ============================================================================
// Author: Daniil Vdovin
// Project: SSRFGuard
// Description: Configuration options for SSRF protection
// License: MIT License
// Copyright (c) 2026 Daniil Vdovin
// ============================================================================

namespace SSRFGuard;

/// <summary>
/// Represents configuration options for SSRF (Server-Side Request Forgery) protection.
/// This class allows customization of the security rules applied to HTTP requests.
/// </summary>
public class SsrfGuardOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether SSRF protection is enabled.
    /// When disabled, all validation is bypassed.
    /// Default value is true.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the set of allowed URL schemes.
    /// Only requests with these schemes will be permitted.
    /// Default value includes "http" and "https".
    /// </summary>
    public HashSet<string> AllowedSchemes { get; set; } = new() { "http", "https" };
    
    /// <summary>
    /// Gets or sets the set of allowed domain names.
    /// Supports wildcard patterns (e.g., "*.example.com").
    /// If empty, domain validation is skipped.
    /// </summary>
    public HashSet<string> AllowedDomains { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the timeout for HTTP requests.
    /// If specified, overrides the default HttpClient timeout.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
    
    /// <summary>
    /// Gets or sets the whitelist of allowed ports.
    /// If specified and not empty, only these ports are permitted.
    /// An empty set means no port restrictions.
    /// </summary>
    public HashSet<int> AllowedPorts { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the blacklist of blocked ports.
    /// Explicitly blocks the specified ports regardless of other settings.
    /// </summary>
    public HashSet<int> BlockedPorts { get; set; } = new();
    
    /// <summary>
    /// Gets or sets a value indicating whether to automatically block well-known service ports.
    /// Includes: SSH(22), Telnet(23), SMTP(25), DNS(53), LDAP(389/636), 
    /// MySQL(3306), PostgreSQL(5432), Redis(6379), MongoDB(27017), etc.
    /// Default value is true.
    /// </summary>
    public bool BlockWellKnownServices { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the mapping of standard ports for URL schemes.
    /// Used to block non-standard ports for specific schemes.
    /// For example, blocks port 8080 for "https" scheme.
    /// Default values: http=80, https=443.
    /// </summary>
    public Dictionary<string, int> StandardPorts { get; set; } = new()
    {
        { "http", 80 },
        { "https", 443 }
    };
    
    /// <summary>
    /// Gets or sets the maximum allowed port number.
    /// Default value is 65535 (maximum valid port).
    /// </summary>
    public int MaxPort { get; set; } = 65535;
    
    /// <summary>
    /// Gets or sets the minimum allowed port number.
    /// Default value is 1 (minimum valid port).
    /// </summary>
    public int MinPort { get; set; } = 1;
}
