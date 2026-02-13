using SSRFGuard;
using SSRFGuard.Exceptions;
using Xunit;

namespace SSRFGuard.Tests;

public class UrlValidatorTests
{
    private readonly SsrfGuardOptions _defaultOptions = new();

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

    [Theory]
    [InlineData("http://example.com:8080")]
    [InlineData("https://example.com:8443")]
    public void AlternativePorts_ShouldPass_WhenNotBlocked(string url)
    {
        var validator = new UrlValidator(_defaultOptions);
        validator.Validate(url); // Should pass by default
    }

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

    [Fact]
    public void NonStandardPortForScheme_ShouldBeAllowed_ByDefault()
    {
        var options = new SsrfGuardOptions
        {
            BlockWellKnownServices = true
        };

        var validator = new UrlValidator(options);
        
        // Нестандартные порты разрешены по умолчанию, если они не являются опасными сервисами
        validator.Validate("http://example.com:8080"); // Should pass
        validator.Validate("https://example.com:8443"); // Should pass
    }

    [Fact]
    public void NonStandardPort_ShouldBeAllowed_WhenInWhitelist()
    {
        var options = new SsrfGuardOptions
        {
            BlockWellKnownServices = true,
            AllowedPorts = new HashSet<int> { 8080 }
        };

        var validator = new UrlValidator(options);
        
        validator.Validate("http://example.com:8080"); // OK - в белом списке
    }

    [Fact]
    public void DisableWellKnownServicesBlock_ShouldAllowAllPorts()
    {
        var options = new SsrfGuardOptions
        {
            BlockWellKnownServices = false
        };

        var validator = new UrlValidator(options);
        
        // Даже опасные порты должны проходить, если проверка отключена
        validator.Validate("http://example.com:3306");
        validator.Validate("http://example.com:6379");
    }
}
