# SSRFGuard

[![en](https://img.shields.io/badge/lang-en-red.svg)](https://github.com/DaniilVdovin/SSRFGuard/blob/master/README.md)
[![ru](https://img.shields.io/badge/lang-ru-green.svg)](https://github.com/DaniilVdovin/SSRFGuard/blob/master/README-RU.md)

Легкая защита от SSRF (подделки запросов на стороне сервера) для HttpClient в микросервисах .NET.

## Зачем нужен SSRFGuard?

Если ваш сервис отправляет исходящие HTTP-запросы на основе пользовательского ввода (например, вебхуки, средства загрузки изображений, прокси-конечные точки), он уязвим для SSRF-атак:

```csharp
// ❌ Уязвимый код
var url = Request.Query["url"]; // "http://169.254.169.254/latest/meta-data"
var content = await _httpClient.GetStringAsync(url); // Утечка учетных данных AWS!
```

SSRFGuard блокирует опасные запросы до того, как они покинут ваш сервис.

## Возможности
- 🔒 Блокирует частные IP-адреса (127.0.0.1, 192.168.x.x, 10.x.x.x, 169.254.x.x)
- 🚫 Предотвращает доступ к конечным точкам метаданных (169.254.169.254, 127.0.0.1, localhost)
- 📋 Белый список доменов с поддержкой подстановочных знаков (*.trusted.com)
- 🔄 Работает как оболочка, сервис внедрения зависимостей или DelegatingHandler

## Использование
### Вариант 1. Простая оболочка (для быстрого старта)
```csharp
var options = new SsrfGuardOptions
{
    AllowedDomains = new HashSet<string> { "api.example.com", "*.trusted.com" }
};

var client = new SafeHttpClient(options);
var response = await client.GetAsync("https://api.example.com/data");
```
### Вариант 2. Внедрение зависимостей (рекомендуется)
```csharp
// Program.cs
builder.Services.AddSsrfGuard(options =>
{
    options.AllowedDomains.Add("api.example.com");
    options.Timeout = TimeSpan.FromSeconds(30);
});

// YourService.cs
public class MyService
{
    private readonly SafeHttpClient _client;
    
    public MyService(SafeHttpClient client) => _client = client;
    
    public async Task<string> FetchDataAsync(string url)
    {
        // Выдает исключение SsrfValidationException для опасных URL
        var response = await _client.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }
}
```

### Вариант 3: интеграция с фабрикой HttpClient
```csharp
builder.Services.AddSsrfGuardHttpClient("SafeExternalClient", options =>
{
    options.AllowedDomains.Add("*.payment-gateway.com");
});

// Использование
var client = _httpClientFactory.CreateClient("SafeExternalClient");
await client.GetAsync(userProvidedUrl); // Защищено!
```
