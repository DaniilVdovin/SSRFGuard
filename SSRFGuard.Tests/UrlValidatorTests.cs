// ============================================================================
// Author: Daniil Vdovin
// Project: SSRFGuard
// Description: Unit tests for URL validator
// License: MIT License
// Copyright (c) 2026 Daniil Vdovin
// ============================================================================

using SSRFGuard;
using SSRFGuard.Exceptions;
using Xunit;

namespace SSRFGuard.Tests;

/// <summary>
/// Unit tests for the <see cref="UrlValidator"/> class.
/// Tests various scenarios including valid URLs, dangerous URLs,
/// domain whitelisting, and port validation.
/// </summary>
public class UrlValidatorTests
{
    /// <summary>
    /// Default SSRF guard options for testing.
    /// </summary>
    private readonly SsrfGuardOptions _defaultOptions = new();

    /// <summary>
    /// Tests that valid URLs pass validation without throwing exceptions.
    /// </summary>
    /// <param name="url">The URL to test.</param>
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://api.service.com")]
    [InlineData("http://example.com:80")]
    [InlineData("https://api.service.com:443")]
    public void ValidUrls_ShouldPass(string url)
    {
        var validator = new UrlValidator(_defaultOptions);
        validator.Validate(url); // No exception
    }

    /// <summary>
    /// Tests that dangerous URLs (localhost, private IPs, file protocols)
    /// throw <see cref="SsrfValidationException"/>.
    /// </summary>
    /// <param name="url">The dangerous URL to test.</param>
    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://localhost:8080")]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://127.1.1.1")]
    [InlineData("http://[::1]")]
    [InlineData("http://[::1]:3000")]
    [InlineData("http://192.168.1.1")]
    [InlineData("http://10.0.0.1")]
    [InlineData("http://169.254.169.254")]
    [InlineData("file:///etc/passwd")]
    public void DangerousUrls_ShouldThrow(string url)
    {
        var validator = new UrlValidator(_defaultOptions);
        Assert.Throws<SsrfValidationException>(() => validator.Validate(url));
    }

    /// <summary>
    /// Tests that domain whitelisting works correctly with exact matches
    /// and wildcard patterns.
    /// </summary>
    [Fact]
    public void AllowedDomains_ShouldWork()
    {
        var options = new SsrfGuardOptions
        {
            AllowedDomains = new HashSet<string> { "api.example.com", "*.trusted.com" }
        };

        var validator = new UrlValidator(options);
        
        validator.Validate("https://api.example.com/data"); // OK
        validator.Validate("https://sub.trusted.com/api"); // OK
        Assert.Throws<SsrfValidationException>(() =>
            validator.Validate("https://evil.com"));
    }

    /// <summary>
    /// Tests that well-known service ports are blocked by default.
    /// </summary>
    /// <param name="url">The URL with a blocked service port.</param>
    [Theory]
    [InlineData("http://example.com:22")]
    [InlineData("http://example.com:23")]
    [InlineData("http://example.com:25")]
    [InlineData("http://example.com:53")]
    [InlineData("http://example.com:3306")]
    [InlineData("http://example.com:5432")]
    [InlineData("http://example.com:6379")]
    [InlineData("http://example.com:27017")]
    public void BlockedServicePorts_ShouldThrow(string url)
    {
        var validator = new UrlValidator(_defaultOptions);
        Assert.Throws<SsrfValidationException>(() => validator.Validate(url));
    }

    /// <summary>
    /// Tests that alternative HTTP/HTTPS ports are allowed by default
    /// when they are not in the blocked service ports list.
    /// </summary>
    /// <param name="url">The URL with an alternative port.</param>
    [Theory]
    [InlineData("http://example.com:8080")]
    [InlineData("https://example.com:8443")]
    public void AlternativePorts_ShouldPass_WhenNotBlocked(string url)
    {
        var validator = new UrlValidator(_defaultOptions);
        validator.Validate(url); // Should pass by default
    }

    /// <summary>
    /// Tests that port whitelisting works correctly.
    /// Only ports in the whitelist should be allowed.
    /// </summary>
    [Fact]
    public void AllowedPorts_Whitelist_ShouldWork()
    {
        var options = new SsrfGuardOptions
        {
            AllowedPorts = new HashSet<int> { 80, 443, 8080 }
        };

        var validator = new UrlValidator(options);
        
        validator.Validate("http://example.com:80"); // OK
        validator.Validate("http://example.com:8080"); // OK
        
        Assert.Throws<SsrfValidationException>(() => 
            validator.Validate("http://example.com:3306"));
    }

    /// <summary>
    /// Tests that port blacklisting works correctly.
    /// Ports in the blacklist should be blocked.
    /// </summary>
    [Fact]
    public void BlockedPorts_Blacklist_ShouldWork()
    {
        var options = new SsrfGuardOptions
        {
            BlockedPorts = new HashSet<int> { 8080, 8443 }
        };

        var validator = new UrlValidator(options);
        
        Assert.Throws<SsrfValidationException>(() => 
            validator.Validate("http://example.com:8080"));
        
        validator.Validate("http://example.com:80"); // OK
    }

    /// <summary>
    /// Tests that custom port range validation works correctly.
    /// Ports outside the specified range should be blocked.
    /// </summary>
    [Fact]
    public void PortRange_ShouldBeValidated()
    {
        var options = new SsrfGuardOptions
        {
            MinPort = 10000,
            MaxPort = 20000
        };

        var validator = new UrlValidator(options);
        
        Assert.Throws<SsrfValidationException>(() => 
            validator.Validate("http://example.com:8080")); // Below min
        
        validator.Validate("http://example.com:15000"); // OK - within range
    }

    /// <summary>
    /// Tests that non-standard ports for schemes are allowed by default
    /// when they are not well-known service ports.
    /// </summary>
    [Fact]
    public void NonStandardPortForScheme_ShouldBeAllowed_ByDefault()
    {
        var options = new SsrfGuardOptions
        {
            BlockWellKnownServices = true
        };

        var validator = new UrlValidator(options);
        
        // Non-standard ports are allowed by default if they are not dangerous services
        validator.Validate("http://example.com:8080"); // Should pass
        validator.Validate("https://example.com:8443"); // Should pass
    }

    /// <summary>
    /// Tests that non-standard ports are allowed when explicitly added to the whitelist.
    /// </summary>
    [Fact]
    public void NonStandardPort_ShouldBeAllowed_WhenInWhitelist()
    {
        var options = new SsrfGuardOptions
        {
            BlockWellKnownServices = true,
            AllowedPorts = new HashSet<int> { 8080 }
        };

        var validator = new UrlValidator(options);
        
        validator.Validate("http://example.com:8080"); // OK - in whitelist
    }

    /// <summary>
    /// Tests that disabling well-known services blocking allows all ports.
    /// </summary>
    [Fact]
    public void DisableWellKnownServicesBlock_ShouldAllowAllPorts()
    {
        var options = new SsrfGuardOptions
        {
            BlockWellKnownServices = false
        };

        var validator = new UrlValidator(options);
        
        // Even dangerous ports should pass when the check is disabled
        validator.Validate("http://example.com:3306");
        validator.Validate("http://example.com:6379");
    }
}
