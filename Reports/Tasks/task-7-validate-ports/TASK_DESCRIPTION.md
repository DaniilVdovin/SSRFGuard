# Task #7: Port Validation Enhancement

## Overview
Enhance SSRFGuard library with comprehensive port validation system to protect against SSRF attacks targeting internal services via non-standard ports.

## Objective
Extend SSRFGuard protection capabilities beyond host/IP validation to include port-level security controls.

## Requirements

### 1. Block Well-Known Service Ports
- Automatically block 20+ ports commonly used by internal services
- **Default behavior:** Enabled (`BlockWellKnownServices = true`)

### 2. Port Whitelist
- Allow only explicitly specified ports
- Type: `HashSet<int>`
- Default: Empty (no restrictions beyond well-known services)

### 3. Port Blacklist  
- Explicitly block specified ports
- Type: `HashSet<int>`
- Default: Empty

### 4. Port Range Validation
- Configure minimum allowed port (`MinPort`, default: 1)
- Configure maximum allowed port (`MaxPort`, default: 65535)

### 5. Standard Ports Configuration
- Define standard ports for URL schemes
- Default: HTTP(80), HTTPS(443)

## Blocked Ports by Default

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
- LDAP/LDAPS: 389, 636

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

## Configuration Properties

```csharp
public HashSet<int> AllowedPorts { get; set; } = new();
public HashSet<int> BlockedPorts { get; set; } = new();
public bool BlockWellKnownServices { get; set; } = true;
public Dictionary<string, int> StandardPorts { get; set; } = new()
{
    { "http", 80 },
    { "https", 443 }
};
public int MinPort { get; set; } = 1;
public int MaxPort { get; set; } = 65535;
```

## Validation Order
1. Port range validation (MinPort-MaxPort)
2. Whitelist validation (AllowedPorts)
3. Blacklist validation (BlockedPorts)
4. Well-known services blocking (BlockWellKnownServices)

## Acceptance Criteria
- ✅ Backward compatible (existing code works without changes)
- ✅ All existing tests pass
- ✅ New tests cover all validation scenarios
- ✅ Build successful with 0 errors and 0 warnings
- ✅ Comprehensive documentation provided

## Priority
**High** - Critical security enhancement

## Estimated Effort
2-3 hours

---
**Created:** February 13, 2026  
**Author:** DaniilVdovin  
**Status:** Completed
