// ============================================================================
// Author: Daniil Vdovin
// Project: SSRFGuard
// Description: Extension methods for dependency injection and HttpClient integration
// License: MIT License
// Copyright (c) 2026 Daniil Vdovin
// ============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SSRFGuard.Extensions;

/// <summary>
/// Provides extension methods for registering SSRF guard services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SSRF guard services to the service collection.
    /// Registers SsrfGuardOptions as a singleton, and UrlValidator and SafeHttpClient as transient services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Optional action to configure SSRF guard options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddSsrfGuard(
        this IServiceCollection services,
        Action<SsrfGuardOptions>? configureOptions = null)
    {
        var options = new SsrfGuardOptions();
        configureOptions?.Invoke(options);

        services.TryAddSingleton<SsrfGuardOptions>(options);
        services.TryAddTransient<UrlValidator>();
        services.TryAddTransient<SafeHttpClient>();

        return services;
    }

    /// <summary>
    /// Adds a named HttpClient with SSRF guard protection to the service collection.
    /// Uses a delegating handler to validate all requests made through this client.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="name">The name of the HttpClient.</param>
    /// <param name="configureOptions">Optional action to configure SSRF guard options.</param>
    /// <returns>The HttpClient builder for further configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or name is null.</exception>
    public static IHttpClientBuilder AddSsrfGuardHttpClient(
        this IServiceCollection services,
        string name,
        Action<SsrfGuardOptions>? configureOptions = null)
    {
        var options = new SsrfGuardOptions();
        configureOptions?.Invoke(options);

        return services
            .AddHttpClient(name)
            .ConfigureHttpClient((_, client) =>
            {
                if (options.Timeout.HasValue)
                    client.Timeout = options.Timeout.Value;
            })
            .AddHttpMessageHandler(_ => new SsrfGuardDelegatingHandler(options));
    }
}

/// <summary>
/// A delegating handler that validates HTTP requests against SSRF protection rules.
/// This handler intercepts requests and validates their URIs before forwarding them.
/// </summary>
public class SsrfGuardDelegatingHandler : DelegatingHandler
{
    /// <summary>
    /// The URL validator used to check request URIs.
    /// </summary>
    private readonly UrlValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="SsrfGuardDelegatingHandler"/> class.
    /// </summary>
    /// <param name="options">The SSRF guard configuration options.</param>
    public SsrfGuardDelegatingHandler(SsrfGuardOptions options)
    {
        _validator = new UrlValidator(options);
    }

    /// <summary>
    /// Sends an HTTP request message to the inner handler after validating the request URI.
    /// </summary>
    /// <param name="request">The HTTP request message to send.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains the HTTP response message.</returns>
    /// <exception cref="SsrfValidationException">Thrown when the request URI fails SSRF validation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri != null)
            _validator.Validate(request.RequestUri.ToString());

        return await base.SendAsync(request, cancellationToken);
    }
}
