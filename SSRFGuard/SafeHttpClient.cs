using System.Net.Http.Headers;
using SSRFGuard.Exceptions;

namespace SSRFGuard;

public class SafeHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly UrlValidator _validator;
    private readonly bool _ownsHttpClient;

    public SafeHttpClient(
        HttpClient httpClient,
        SsrfGuardOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _validator = new UrlValidator(options);
        _ownsHttpClient = false;
    }

    public SafeHttpClient(SsrfGuardOptions options)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false // Предотвращаем редиректы на внутренние ресурсы
        };

        _httpClient = new HttpClient(handler);
        _validator = new UrlValidator(options);
        _ownsHttpClient = true;

        if (options.Timeout.HasValue)
            _httpClient.Timeout = options.Timeout.Value;
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        _validator.Validate(url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
    {
        _validator.Validate(url);
        return await _httpClient.PostAsync(url, content);
    }

    public async Task<HttpResponseMessage> PutAsync(string url, HttpContent content)
    {
        _validator.Validate(url);
        return await _httpClient.PutAsync(url, content);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        _validator.Validate(url);
        return await _httpClient.DeleteAsync(url);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        if (request.RequestUri != null)
            _validator.Validate(request.RequestUri.ToString());
        
        return await _httpClient.SendAsync(request);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}