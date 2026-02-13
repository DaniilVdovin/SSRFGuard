// ============================================================================
// Author: Daniil Vdovin
// Project: SSRFGuard
// Description: Safe HTTP client wrapper with SSRF protection
// License: MIT License
// Copyright (c) 2026 Daniil Vdovin
// ============================================================================

using System.Net.Http.Headers;
using SSRFGuard.Exceptions;

namespace SSRFGuard;

/// <summary>
/// Provides a safe HTTP client wrapper that validates URLs before making requests
/// to prevent Server-Side Request Forgery (SSRF) attacks.
/// </summary>
public class SafeHttpClient : IDisposable
{
    /// <summary>
    /// The underlying HttpClient instance used for making HTTP requests.
    /// </summary>
    private readonly HttpClient _httpClient;
    
    /// <summary>
    /// The URL validator responsible for checking URLs against SSRF rules.
    /// </summary>
    private readonly UrlValidator _validator;
    
    /// <summary>
    /// Indicates whether this instance owns the HttpClient and should dispose it.
    /// </summary>
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeHttpClient"/> class
    /// using an existing HttpClient instance.
    /// </summary>
    /// <param name="httpClient">The existing HttpClient to wrap. Must not be null.</param>
    /// <param name="options">The SSRF guard configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient is null.</exception>
    public SafeHttpClient(
        HttpClient httpClient,
        SsrfGuardOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _validator = new UrlValidator(options);
        _ownsHttpClient = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeHttpClient"/> class
    /// with a new internal HttpClient.
    /// </summary>
    /// <param name="options">The SSRF guard configuration options.</param>
    public SafeHttpClient(SsrfGuardOptions options)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false // Prevent redirects to internal resources
        };

        _httpClient = new HttpClient(handler);
        _validator = new UrlValidator(options);
        _ownsHttpClient = true;

        if (options.Timeout.HasValue)
            _httpClient.Timeout = options.Timeout.Value;
    }

    /// <summary>
    /// Sends a GET request to the specified URL asynchronously.
    /// </summary>
    /// <param name="url">The URL to send the request to.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains the HTTP response message.</returns>
    /// <exception cref="SsrfValidationException">Thrown when the URL fails SSRF validation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when url is null or empty.</exception>
    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        _validator.Validate(url);
        return await _httpClient.GetAsync(url);
    }

    /// <summary>
    /// Sends a POST request to the specified URL asynchronously.
    /// </summary>
    /// <param name="url">The URL to send the request to.</param>
    /// <param name="content">The HTTP request content sent to the server.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains the HTTP response message.</returns>
    /// <exception cref="SsrfValidationException">Thrown when the URL fails SSRF validation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when url is null or empty.</exception>
    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
    {
        _validator.Validate(url);
        return await _httpClient.PostAsync(url, content);
    }

    /// <summary>
    /// Sends a PUT request to the specified URL asynchronously.
    /// </summary>
    /// <param name="url">The URL to send the request to.</param>
    /// <param name="content">The HTTP request content sent to the server.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains the HTTP response message.</returns>
    /// <exception cref="SsrfValidationException">Thrown when the URL fails SSRF validation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when url is null or empty.</exception>
    public async Task<HttpResponseMessage> PutAsync(string url, HttpContent content)
    {
        _validator.Validate(url);
        return await _httpClient.PutAsync(url, content);
    }

    /// <summary>
    /// Sends a DELETE request to the specified URL asynchronously.
    /// </summary>
    /// <param name="url">The URL to send the request to.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains the HTTP response message.</returns>
    /// <exception cref="SsrfValidationException">Thrown when the URL fails SSRF validation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when url is null or empty.</exception>
    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        _validator.Validate(url);
        return await _httpClient.DeleteAsync(url);
    }

    /// <summary>
    /// Sends an HTTP request message asynchronously.
    /// </summary>
    /// <param name="request">The HTTP request message to send.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains the HTTP response message.</returns>
    /// <exception cref="SsrfValidationException">Thrown when the request URI fails SSRF validation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        if (request.RequestUri != null)
            _validator.Validate(request.RequestUri.ToString());
        
        return await _httpClient.SendAsync(request);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="SafeHttpClient"/>
    /// and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}