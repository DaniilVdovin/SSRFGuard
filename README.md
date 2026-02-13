# SSRFGuard

<img src="/baner.png"></img>

[![en](https://img.shields.io/badge/lang-en-red.svg)](https://github.com/DaniilVdovin/SSRFGuard/blob/master/README.md)
[![ru](https://img.shields.io/badge/lang-ru-green.svg)](https://github.com/DaniilVdovin/SSRFGuard/blob/master/README-RU.md)

Lightweight SSRF (Server-Side Request Forgery) protection for HttpClient in .NET microservices.

## Why SSRFGuard?

When your service makes outbound HTTP requests based on user input (e.g., webhooks, image fetchers, proxy endpoints), you're vulnerable to SSRF attacks:

```csharp
// ❌ Vulnerable code
var url = Request.Query["url"]; // "http://169.254.169.254/latest/meta-data"
var content = await _httpClient.GetStringAsync(url); // Leaks AWS credentials!
```

SSRFGuard blocks dangerous requests before they leave your service.

## Features
- 🔒 Blocks private IPs (127.0.0.1, 192.168.x.x, 10.x.x.x, 169.254.x.x)
- 🚫 Prevents metadata endpoint access (169.254.169.254, 127.0.0.1, localhost)
- 📋 Domain whitelist with wildcard support (*.trusted.com)
- 🔄 Works as wrapper, DI service, or DelegatingHandler
- 🚪 Port validation with whitelist/blacklist support (Task 7)
- ⚠️ Blocks 20+ dangerous service ports by default (Task 7)

## Usage
### Option 1: Simple wrapper (quick start)
```csharp
var options = new SsrfGuardOptions
{
    AllowedDomains = new HashSet<string> { "api.example.com", "*.trusted.com" },
    
    // Port validation (Task 7)
    AllowedPorts = new HashSet<int> { 80, 443 },
    BlockedPorts = new HashSet<int> { 22, 3306, 6379 },
    BlockWellKnownServices = true
};

var client = new SafeHttpClient(options);
var response = await client.GetAsync("https://api.example.com/data");
```
### Option 2: Dependency Injection (recommended)
```csharp
// Program.cs
builder.Services.AddSsrfGuard(options =>
{
    options.AllowedDomains.Add("api.example.com");
    options.Timeout = TimeSpan.FromSeconds(30);
    
    // Port validation (Task 7)
    options.AllowedPorts = new HashSet<int> { 80, 443 };
    options.BlockedPorts = new HashSet<int> { 22, 3306, 6379 };
    options.BlockWellKnownServices = true; // Blocks 20+ dangerous ports by default
});

// YourService.cs
public class MyService
{
    private readonly SafeHttpClient _client;
    
    public MyService(SafeHttpClient client) => _client = client;
    
    public async Task<string> FetchDataAsync(string url)
    {
        // Will throw SsrfValidationException for dangerous URLs
        var response = await _client.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }
}
```

### Option 3: HttpClient Factory integration
```csharp
builder.Services.AddSsrfGuardHttpClient("SafeExternalClient", options =>
{
    options.AllowedDomains.Add("*.payment-gateway.com");
    
    // Port validation (Task 7)
    options.AllowedPorts = new HashSet<int> { 80, 443 };
    options.BlockWellKnownServices = true;
});

// Usage
var client = _httpClientFactory.CreateClient("SafeExternalClient");
await client.GetAsync(userProvidedUrl); // Protected!
```
