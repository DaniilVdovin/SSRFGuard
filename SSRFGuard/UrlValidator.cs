using System.Net;
using SSRFGuard.Exceptions;

namespace SSRFGuard;

public class UrlValidator
{
    private readonly SsrfGuardOptions _options;

    public UrlValidator(SsrfGuardOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public void Validate(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));

        if (!_options.Enabled)
            return;

        var uri = new Uri(url);
        

        // Блокируем файловые протоколы
        if (uri.IsFile || uri.IsUnc)
            throw new SsrfValidationException(url, "File protocols are not allowed");

        // Разрешаем только указанные схемы
        if (!_options.AllowedSchemes.Contains(uri.Scheme))
            throw new SsrfValidationException(url, $"Scheme '{uri.Scheme}' is not allowed");

        // Получаем хост
        string host = uri.Host;
        
        if (IsDangerousHostname(host))
            throw new SsrfValidationException(url, $"Dangerous hostname '{host}' is not allowed");

        // Проверяем белый список доменов
        if (_options.AllowedDomains.Any() && !IsHostAllowed(host))
            throw new SsrfValidationException(url, $"Host '{host}' is not in allowed domains");

        // Блокируем потенциально опасные IP
        if (IPAddress.TryParse(host, out var ip))
        {
            if (IsPrivateIpAddress(ip))
                throw new SsrfValidationException(url, $"Private IP address '{ip}' is not allowed");

            if (IsReservedIpAddress(ip))
                throw new SsrfValidationException(url, $"Reserved IP address '{ip}' is not allowed");
        }

        // Валидация порта
        ValidatePort(uri, url);
    }
    
    private void ValidatePort(Uri uri, string url)
    {
        // Порт -1 означает, что порт не указан явно в URL
        int port = uri.Port;
        
        // Если порт не указан, используем стандартный порт для схемы
        if (port == -1 && _options.StandardPorts.TryGetValue(uri.Scheme, out int defaultPort))
        {
            port = defaultPort;
        }
        
        // Если порт все еще не определен, пропускаем проверку
        if (port == -1)
            return;
        
        // Проверяем диапазон портов
        if (port < _options.MinPort || port > _options.MaxPort)
            throw new SsrfValidationException(url, $"Port {port} is outside allowed range ({_options.MinPort}-{_options.MaxPort})");
        
        // Проверяем белый список портов
        if (_options.AllowedPorts.Any() && !_options.AllowedPorts.Contains(port))
            throw new SsrfValidationException(url, $"Port {port} is not in allowed ports list");
        
        // Проверяем черный список портов
        if (_options.BlockedPorts.Contains(port))
            throw new SsrfValidationException(url, $"Port {port} is explicitly blocked");
        
        // Блокируем известные порты внутренних сервисов
        if (_options.BlockWellKnownServices && IsWellKnownServicePort(port))
            throw new SsrfValidationException(url, $"Port {port} is a well-known service port and is blocked");
    }
    
    private bool IsWellKnownServicePort(int port)
    {
        // Известные порты внутренних сервисов
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
            8080 => false, // HTTP alternative (разрешаем, но можно настроить)
            8443 => false, // HTTPS alternative (разрешаем, но можно настроить)
            _ => false
        };
    }
    
    private bool IsDangerousHostname(string host)
    {
        var normalized = host.ToLowerInvariant();
        return normalized == "localhost" ||
            normalized == "localhost.localdomain" ||
            normalized == "ip6-localhost" ||
            normalized == "[::1]" || // IPv6 в квадратных скобках (как в URL)
            normalized.StartsWith("127.") ||
            normalized.StartsWith("[::1"); // Частичное совпадение для [::1]:port
    }

    private bool IsHostAllowed(string host)
    {
        foreach (var allowed in _options.AllowedDomains)
        {
            if (host.Equals(allowed, StringComparison.OrdinalIgnoreCase))
                return true;

            // Поддержка wildcard (*.example.com)
            if (allowed.StartsWith("*.", StringComparison.OrdinalIgnoreCase))
            {
                var suffix = allowed[1..]; // ".example.com"
                if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

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