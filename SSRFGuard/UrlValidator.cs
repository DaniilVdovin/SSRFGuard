// ============================================================================
// Author: Daniil Vdovin
// Project: SSRFGuard
// Description: URL validator for SSRF protection
// License: MIT License
// Copyright (c) 2026 Daniil Vdovin
// ============================================================================

using System.Net;
using SSRFGuard.Exceptions;

namespace SSRFGuard;

/// <summary>
/// Validates URLs to prevent Server-Side Request Forgery (SSRF) attacks.
/// Checks URLs against security rules including scheme validation, 
/// hostname/IP validation, and domain whitelisting.
/// </summary>
public class UrlValidator
{
    /// <summary>
    /// The SSRF guard configuration options used for validation.
    /// </summary>
    private readonly SsrfGuardOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlValidator"/> class.
    /// </summary>
    /// <param name="options">The SSRF guard configuration options. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public UrlValidator(SsrfGuardOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Validates a URL against SSRF protection rules.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when url is null or empty.</exception>
    /// <exception cref="SsrfValidationException">Thrown when the URL fails any SSRF validation rule.</exception>
    public void Validate(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));

        if (!_options.Enabled)
            return;

        var uri = new Uri(url);
        

        // Block file protocols
        if (uri.IsFile || uri.IsUnc)
            throw new SsrfValidationException(url, "File protocols are not allowed");

        // Allow only specified schemes
        if (!_options.AllowedSchemes.Contains(uri.Scheme))
            throw new SsrfValidationException(url, $"Scheme '{uri.Scheme}' is not allowed");

        // Get the host
        string host = uri.Host;
        
        if (IsDangerousHostname(host))
            throw new SsrfValidationException(url, $"Dangerous hostname '{host}' is not allowed");

        // Check domain whitelist
        if (_options.AllowedDomains.Any() && !IsHostAllowed(host))
            throw new SsrfValidationException(url, $"Host '{host}' is not in allowed domains");

        // Block potentially dangerous IPs
        if (IPAddress.TryParse(host, out var ip))
        {
            if (IsPrivateIpAddress(ip))
                throw new SsrfValidationException(url, $"Private IP address '{ip}' is not allowed");

            if (IsReservedIpAddress(ip))
                throw new SsrfValidationException(url, $"Reserved IP address '{ip}' is not allowed");
        }

        // Validate port
        ValidatePort(uri, url);
    }
    
    /// <summary>
    /// Validates the port number from a URI against port security rules.
    /// </summary>
    /// <param name="uri">The URI containing the port to validate.</param>
    /// <param name="url">The original URL string for error reporting.</param>
    /// <exception cref="SsrfValidationException">Thrown when the port fails validation.</exception>
    private void ValidatePort(Uri uri, string url)
    {
        // Port -1 means the port is not explicitly specified in the URL
        int port = uri.Port;
        
        // If port is not specified, use the standard port for the scheme
        if (port == -1 && _options.StandardPorts.TryGetValue(uri.Scheme, out int defaultPort))
        {
            port = defaultPort;
        }
        
        // If port is still not determined, skip validation
        if (port == -1)
            return;
        
        // Check port range
        if (port < _options.MinPort || port > _options.MaxPort)
            throw new SsrfValidationException(url, $"Port {port} is outside allowed range ({_options.MinPort}-{_options.MaxPort})");
        
        // Check whitelist of allowed ports
        if (_options.AllowedPorts.Any() && !_options.AllowedPorts.Contains(port))
            throw new SsrfValidationException(url, $"Port {port} is not in allowed ports list");
        
        // Check blacklist of blocked ports
        if (_options.BlockedPorts.Contains(port))
            throw new SsrfValidationException(url, $"Port {port} is explicitly blocked");
        
        // Block well-known internal service ports
        if (_options.BlockWellKnownServices && IsWellKnownServicePort(port))
            throw new SsrfValidationException(url, $"Port {port} is a well-known service port and is blocked");
    }
    
    /// <summary>
    /// Determines whether a port number is a well-known internal service port.
    /// </summary>
    /// <param name="port">The port number to check.</param>
    /// <returns>True if the port is a well-known service port; otherwise, false.</returns>
    private bool IsWellKnownServicePort(int port)
    {
        // Well-known internal service ports
        return port switch
        {
            22 => true,    // SSH
            23 => true,    // Telnet
            25 => true,    // SMTP
            53 => true,    // DNS
            110 => true,   // POP3
            143 => true,   // IMAP
            389 => true,   // LDAP
            445 => true,   // SMB
            636 => true,   // LDAPS
            1433 => true,  // Microsoft SQL Server
            1521 => true,  // Oracle Database
            27017 => true, // MongoDB
            3306 => true,  // MySQL
            3389 => true,  // RDP
            5432 => true,  // PostgreSQL
            5900 => true,  // VNC
            6379 => true,  // Redis
            8080 => false, // HTTP alternative (allowed, but can be configured)
            8443 => false, // HTTPS alternative (allowed, but can be configured)
            _ => false
        };
    }
    
    /// <summary>
    /// Determines whether a hostname is considered dangerous and should be blocked.
    /// </summary>
    /// <param name="host">The hostname to check.</param>
    /// <returns>True if the hostname is dangerous; otherwise, false.</returns>
    private bool IsDangerousHostname(string host)
    {
        var normalized = host.ToLowerInvariant();
        return normalized == "localhost" ||
            normalized == "localhost.localdomain" ||
            normalized == "ip6-localhost" ||
            normalized == "[::1]" || // IPv6 in square brackets (as in URL)
            normalized.StartsWith("127.") ||
            normalized.StartsWith("[::1"); // Partial match for [::1]:port
    }

    /// <summary>
    /// Determines whether a host is allowed based on the domain whitelist.
    /// Supports exact matches and wildcard patterns (e.g., "*.example.com").
    /// </summary>
    /// <param name="host">The host to check.</param>
    /// <returns>True if the host is allowed; otherwise, false.</returns>
    private bool IsHostAllowed(string host)
    {
        foreach (var allowed in _options.AllowedDomains)
        {
            if (host.Equals(allowed, StringComparison.OrdinalIgnoreCase))
                return true;

            // Support wildcard (*.example.com)
            if (allowed.StartsWith("*.", StringComparison.OrdinalIgnoreCase))
            {
                var suffix = allowed[1..]; // ".example.com"
                if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines whether an IP address is a private (internal) IP address.
    /// Checks for IPv4 private ranges (10.x.x.x, 172.16-31.x.x, 192.168.x.x)
    /// and IPv6 private ranges (link-local and site-local).
    /// </summary>
    /// <param name="ip">The IP address to check.</param>
    /// <returns>True if the IP is private; otherwise, false.</returns>
    private bool IsPrivateIpAddress(IPAddress ip)
    {
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            byte[] bytes = ip.GetAddressBytes();
            return (bytes[0] == 10) ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168);
        }

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal;
        }

        return false;
    }

    /// <summary>
    /// Determines whether an IP address is a reserved IP address.
    /// Checks for loopback, any, broadcast addresses, and link-local addresses.
    /// </summary>
    /// <param name="ip">The IP address to check.</param>
    /// <returns>True if the IP is reserved; otherwise, false.</returns>
    private bool IsReservedIpAddress(IPAddress ip)
    {
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            byte[] bytes = ip.GetAddressBytes();
            return ip.Equals(IPAddress.Loopback) ||
                ip.Equals(IPAddress.Any) ||
                ip.Equals(IPAddress.Broadcast) ||
                (bytes[0] == 169 && bytes[1] == 254) || // Link-local (169.254.x.x)
                (bytes[0] == 127); // Loopback range (127.x.x.x)
        }

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return ip.IsIPv6LinkLocal ||
                ip.IsIPv6SiteLocal ||
                ip.Equals(IPAddress.IPv6Loopback) || // ::1
                ip.IsIPv6Multicast;
        }

        return false;
    }
}