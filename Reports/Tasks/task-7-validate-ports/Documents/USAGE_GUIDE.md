# Port Validation - Usage Guide

## Overview

SSRFGuard v1.0.0 includes comprehensive port validation to protect against SSRF attacks targeting internal services via non-standard ports.

## Quick Start

### Basic Protection (Recommended)

```csharp
var options = new SsrfGuardOptions(); // BlockWellKnownServices = true by default

var validator = new UrlValidator(options);
validator.Validate("http://example.com:80");   // ✅ Allowed
validator.Validate("http://example.com:3306"); // ❌ Blocked (MySQL)
```

By default, SSRFGuard automatically blocks 20+ dangerous ports used by internal services.

## Configuration Options

### 1. Block Well-Known Service Ports

**Default:** `true`

Automatically blocks ports commonly used by internal services (databases, network services, etc.).

```csharp
var options = new SsrfGuardOptions
{
    BlockWellKnownServices = true // Default
};

// Blocked ports include: 22, 23, 25, 53, 3306, 5432, 6379, etc.
```

To disable:
```csharp
var options = new SsrfGuardOptions
{
    BlockWellKnownServices = false
};

validator.Validate("http://example.com:3306"); // ✅ Allowed
```

### 2. Port Whitelist

Allow only explicitly specified ports.

```csharp
var options = new SsrfGuardOptions
{
    AllowedPorts = new HashSet<int> { 80, 443, 8080 }
};

validator.Validate("http://example.com:80");   // ✅ Allowed
validator.Validate("http://example.com:443");  // ✅ Allowed
validator.Validate("http://example.com:8080"); // ✅ Allowed
validator.Validate("http://example.com:3306"); // ❌ Blocked
```

**Note:** When whitelist is configured, only ports in the list are allowed (whitelist takes precedence).

### 3. Port Blacklist

Explicitly block specific ports.

```csharp
var options = new SsrfGuardOptions
{
    BlockedPorts = new HashSet<int> { 8080, 9000, 9001 }
};

validator.Validate("http://example.com:80");   // ✅ Allowed
validator.Validate("http://example.com:8080"); // ❌ Blocked
validator.Validate("http://example.com:9000"); // ❌ Blocked
```

### 4. Port Range Validation

Restrict ports to a specific range.

```csharp
var options = new SsrfGuardOptions
{
    MinPort = 10000,
    MaxPort = 20000
};

validator.Validate("http://example.com:80");     // ❌ Below minimum
validator.Validate("http://example.com:15000");  // ✅ Within range
validator.Validate("http://example.com:30000");  // ❌ Above maximum
```

### 5. Standard Ports Configuration

Define standard ports for URL schemes.

```csharp
var options = new SsrfGuardOptions
{
    StandardPorts = new Dictionary<string, int>
    {
        { "http", 80 },
        { "https", 443 },
        { "ftp", 21 }
    }
};
```

## Combined Strategies

### Example 1: Strict Security (Production)

Allow only standard HTTP/HTTPS ports:

```csharp
var options = new SsrfGuardOptions
{
    AllowedPorts = new HashSet<int> { 80, 443 },
    BlockWellKnownServices = true
};

// Only ports 80 and 443 are allowed
validator.Validate("http://example.com:80");   // ✅ Allowed
validator.Validate("https://example.com:443"); // ✅ Allowed
validator.Validate("http://example.com:8080"); // ❌ Blocked
validator.Validate("http://example.com:3306"); // ❌ Blocked
```

### Example 2: Flexible Configuration

Allow common ports + custom internal ports:

```csharp
var options = new SsrfGuardOptions
{
    AllowedPorts = new HashSet<int> { 80, 443, 8080, 8443, 9000, 9001 },
    BlockedPorts = new HashSet<int> { 22, 3306, 6379 }, // Extra protection
    BlockWellKnownServices = true,
    MinPort = 1,
    MaxPort = 65535
};

validator.Validate("http://example.com:8080"); // ✅ Allowed (whitelist)
validator.Validate("http://example.com:9000"); // ✅ Allowed (whitelist)
validator.Validate("http://example.com:3306"); // ❌ Blocked (well-known)
validator.Validate("http://example.com:22");   // ❌ Blocked (blacklist)
```

### Example 3: Development Environment

Allow more flexibility during development:

```csharp
var options = new SsrfGuardOptions
{
    BlockWellKnownServices = true, // Still block dangerous ports
    AllowedPorts = new HashSet<int> { 80, 443, 3000, 5000, 8000, 8080 }
};

// Common development ports allowed
validator.Validate("http://localhost:3000"); // ✅ Allowed
validator.Validate("http://localhost:5000"); // ✅ Allowed
validator.Validate("http://localhost:3306"); // ❌ Blocked (MySQL)
```

## Integration with Dependency Injection

### ASP.NET Core

```csharp
// Program.cs or Startup.cs
builder.Services.AddSsrfGuardHttpClient("SecureApi", options => 
{
    options.AllowedPorts = new HashSet<int> { 443 };
    options.BlockWellKnownServices = true;
    options.AllowedDomains = new HashSet<string> { "api.example.com" };
});

// Usage in controller
public class MyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public MyController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<IActionResult> Get()
    {
        var client = _httpClientFactory.CreateClient("SecureApi");
        var response = await client.GetAsync("https://api.example.com/data");
        return Ok(await response.Content.ReadAsStringAsync());
    }
}
```

### Custom HttpClient

```csharp
var options = new SsrfGuardOptions
{
    AllowedPorts = new HashSet<int> { 443 },
    BlockWellKnownServices = true
};

var httpClient = new SafeHttpClient(options);

// All requests will be validated
var response = await httpClient.GetAsync("https://example.com");
```

## Validation Order

Port validation checks are executed in this order:

1. **Port Range** - Check if port is within MinPort-MaxPort
2. **Whitelist** - If configured, check if port is in AllowedPorts
3. **Blacklist** - If configured, check if port is in BlockedPorts
4. **Well-Known Services** - If enabled, block dangerous ports

**Important:** Whitelist takes precedence over blacklist and well-known services.

## Blocked Ports Reference

### Databases (7 ports)
- MySQL: 3306
- PostgreSQL: 5432
- MongoDB: 27017
- Redis: 6379
- Oracle: 1521
- SQL Server: 1433
- Memcached: 11211

### Network Services (6 ports)
- SSH: 22
- Telnet: 23
- SMTP: 25
- DNS: 53
- LDAP: 389
- LDAPS: 636

### Remote Access (2 ports)
- RDP: 3389
- VNC: 5900

### File Services (1 port)
- SMB: 445

### Other Services (4 ports)
- NetBIOS: 139
- MS RPC: 135
- Kerberos: 88
- Docker Registry: 5000

## Exception Handling

When a URL is blocked, `SsrfValidationException` is thrown:

```csharp
try
{
    validator.Validate("http://example.com:3306");
}
catch (SsrfValidationException ex)
{
    Console.WriteLine($"Blocked: {ex.Message}");
    // Output: "Blocked: Port 3306 is reserved for internal services"
}
```

## Best Practices

### 1. Production Environment

```csharp
var options = new SsrfGuardOptions
{
    AllowedPorts = new HashSet<int> { 443 }, // HTTPS only
    BlockWellKnownServices = true,
    AllowedDomains = new HashSet<string> { "api.example.com", "cdn.example.com" },
    AllowedSchemes = new HashSet<string> { "https" } // HTTPS only
};
```

### 2. Development Environment

```csharp
var options = new SsrfGuardOptions
{
    BlockWellKnownServices = true, // Keep protection
    AllowedPorts = new HashSet<int> { 80, 443, 3000, 5000, 8000, 8080 },
    AllowedDomains = new HashSet<string> { "localhost", "api.example.com" }
};
```

### 3. Internal Services

```csharp
var options = new SsrfGuardOptions
{
    AllowedPorts = new HashSet<int> { 80, 443, 9000, 9001, 9002 }, // Custom ports
    BlockedPorts = new HashSet<int> { 22, 3306, 6379, 27017 }, // Block databases
    BlockWellKnownServices = true,
    MinPort = 1,
    MaxPort = 65535
};
```

## Troubleshooting

### Issue: Legitimate port is blocked

**Solution:** Add the port to the whitelist.

```csharp
options.AllowedPorts.Add(9000); // Allow port 9000
```

### Issue: Need to allow all ports

**Solution:** Disable well-known services blocking (not recommended for production).

```csharp
options.BlockWellKnownServices = false;
```

### Issue: Custom port range needed

**Solution:** Configure MinPort and MaxPort.

```csharp
options.MinPort = 10000;
options.MaxPort = 20000;
```

## Performance Considerations

- Port validation adds minimal overhead (< 1ms per validation)
- Whitelist/blacklist lookups use HashSet for O(1) performance
- Well-known ports check uses predefined array for fast lookup

## Migration from Previous Versions

If you're upgrading from a version without port validation:

1. **No code changes needed** - Backward compatible
2. **Default protection enabled** - Dangerous ports blocked automatically
3. **Customize if needed** - Configure AllowedPorts, BlockedPorts, etc.

```csharp
// Existing code continues to work
var options = new SsrfGuardOptions();
var validator = new UrlValidator(options);

// New: Enhanced protection against port-based SSRF attacks
validator.Validate("http://example.com:3306"); // Now blocked!
```

## Summary

Port validation in SSRFGuard provides:

- ✅ **Automatic protection** - 20+ dangerous ports blocked by default
- ✅ **Flexible configuration** - Whitelist, blacklist, range validation
- ✅ **Easy integration** - Works with existing code
- ✅ **High performance** - Minimal overhead
- ✅ **Comprehensive coverage** - All common attack vectors covered

Choose the configuration that best fits your security requirements!

---
**Version:** 1.0.0  
**Last Updated:** February 13, 2026  
**Author:** DaniilVdovin
