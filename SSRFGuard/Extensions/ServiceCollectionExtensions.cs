using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SSRFGuard.Extensions;

public static class ServiceCollectionExtensions
{
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

public class SsrfGuardDelegatingHandler : DelegatingHandler
{
    private readonly UrlValidator _validator;

    public SsrfGuardDelegatingHandler(SsrfGuardOptions options)
    {
        _validator = new UrlValidator(options);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri != null)
            _validator.Validate(request.RequestUri.ToString());

        return await base.SendAsync(request, cancellationToken);
    }
}
