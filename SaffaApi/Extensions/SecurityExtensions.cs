using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;
using SaffaApi.Services;

namespace SaffaApi.Extensions;

public static class SecurityExtensions
{
    // Pinned Cloudflare proxy ranges — used as the initial state until CloudflareIpService
    // fetches the live list, and as a safety net if the operator doesn't configure anything.
    // Source: https://www.cloudflare.com/ips/
    private static readonly string[] CloudflareFallbackCidrs =
    [
        // IPv4
        "173.245.48.0/20",
        "103.21.244.0/22",
        "103.22.200.0/22",
        "103.31.4.0/22",
        "141.101.64.0/18",
        "108.162.192.0/18",
        "190.93.240.0/20",
        "188.114.96.0/20",
        "197.234.240.0/22",
        "198.41.128.0/17",
        "162.158.0.0/15",
        "104.16.0.0/13",
        "104.24.0.0/14",
        "172.64.0.0/13",
        "131.0.72.0/22",
        // IPv6
        "2400:cb00::/32",
        "2606:4700::/32",
        "2803:f800::/32",
        "2405:b500::/32",
        "2405:8100::/32",
        "2a06:98c0::/29",
        "2c0f:f248::/32",
    ];

    public static IServiceCollection AddSaffaSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        string[]? configuredRanges = configuration
            .GetSection("ForwardedHeaders:TrustedNetworks")
            .Get<string[]>();

        bool hasConfigOverride = configuredRanges is { Length: > 0 };
        string[] seedRanges = hasConfigOverride ? configuredRanges! : CloudflareFallbackCidrs;

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 2; // Cloudflare edge → load balancer

            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (string cidr in seedRanges)
            {
                if (IPNetwork.TryParse(cidr, out IPNetwork network))
                    options.KnownIPNetworks.Add(network);
            }
        });

        // If ranges come from config, they're static — no need to refresh from Cloudflare's API.
        if (!hasConfigOverride)
        {
            services.AddHostedService<CloudflareIpService>();
        }

        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });

        return services;
    }
}
