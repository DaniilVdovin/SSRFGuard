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
}
