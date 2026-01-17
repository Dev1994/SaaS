using Microsoft.AspNetCore.HttpOverrides;

namespace SaffaApi.Extensions;

public static class SecurityExtensions
{
    public static IServiceCollection AddSaffaSecurity(this IServiceCollection services)
    {
        // Configure forwarded headers for Cloudflare
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // Configure HSTS
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(60);
        });

        return services;
    }
}