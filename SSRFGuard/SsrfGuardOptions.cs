namespace SSRFGuard;

public class SsrfGuardOptions
{
    public bool Enabled { get; set; } = true;
    
    public HashSet<string> AllowedSchemes { get; set; } = new() { "http", "https" };
    
    public HashSet<string> AllowedDomains { get; set; } = new();
    
    public TimeSpan? Timeout { get; set; }
    
    /// <summary>
    /// Белый список разрешенных портов. Если указан, все остальные порты блокируются.
    /// Пустой список означает отсутствие ограничений на порты.
    /// </summary>
    public HashSet<int> AllowedPorts { get; set; } = new();
    
    /// <summary>
    /// Черный список запрещенных портов. Явно блокирует указанные порты.
    /// </summary>
    public HashSet<int> BlockedPorts { get; set; } = new();
    
    /// <summary>
    /// Автоматически блокировать известные порты внутренних сервисов.
    /// Включает: SSH(22), Telnet(23), SMTP(25), DNS(53), LDAP(389/636), 
    /// MySQL(3306), PostgreSQL(5432), Redis(6379), MongoDB(27017) и др.
    /// </summary>
    public bool BlockWellKnownServices { get; set; } = true;
    
    /// <summary>
    /// Блокировать нестандартные порты для указанных схем.
    /// Например, блокировать порт 8080 для схемы "https".
    /// </summary>
    public Dictionary<string, int> StandardPorts { get; set; } = new()
    {
        { "http", 80 },
        { "https", 443 }
    };
    
    /// <summary>
    /// Максимально разрешенный порт (по умолчанию 65535)
    /// </summary>
    public int MaxPort { get; set; } = 65535;
    
    /// <summary>
    /// Минимально разрешенный порт (по умолчанию 1)
    /// </summary>
    public int MinPort { get; set; } = 1;
}
