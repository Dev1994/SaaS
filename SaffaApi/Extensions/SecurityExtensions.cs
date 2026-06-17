using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace SaffaApi.Extensions;

public static class SecurityExtensions
{
    // Cloudflare published proxy ranges. Used as the default trust list when none is
    // supplied via configuration. Source: https://www.cloudflare.com/ips/
    private static readonly string[] CloudflareNetworks =
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
        "2c0f:f248::/32"
    ];

    public static IServiceCollection AddSaffaSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure forwarded headers. Only trust X-Forwarded-* from known proxy networks
        // (default: Cloudflare). Trusting all sources would let any client spoof
        // X-Forwarded-For and bypass IP-based rate limiting / poison traces.
        string[] trustedNetworks = configuration.GetSection("ForwardedHeaders:TrustedNetworks").Get<string[]>() is { Length: > 0 } configured
            ? configured
            : CloudflareNetworks;

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Replace defaults (loopback only) with the explicit trusted proxy ranges.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (string cidr in trustedNetworks)
            {
                if (IPNetwork.TryParse(cidr, out IPNetwork network))
                {
                    options.KnownIPNetworks.Add(network);
                }
            }

            // A chain of proxies (e.g. Cloudflare -> load balancer) appends multiple
            // entries to X-Forwarded-For. Allow enough hops for the trusted chain.
            options.ForwardLimit = trustedNetworks.Length > 0 ? null : 1;
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
