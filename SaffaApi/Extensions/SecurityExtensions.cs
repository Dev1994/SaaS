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

    // Hops between Cloudflare and the app. With a cloudflared tunnel the TCP peer is never a
    // Cloudflare IP: in production it is the tunnel container on the shared cloudflared Docker
    // network (172.16.0.0/12), or loopback when the tunnel runs on the host.
    // Trust boundary: every container on a Docker bridge network on the host can set
    // X-Forwarded-For. Narrow this with ForwardedHeaders:LocalProxyNetworks (e.g. the tunnel
    // network's subnet) if untrusted containers share the host. Never publish the app's port
    // through docker-proxy with this default: LAN clients would arrive from the bridge gateway.
    private static readonly string[] DefaultLocalProxyCidrs =
    [
        "127.0.0.0/8",
        "::1/128",
        "172.16.0.0/12",
    ];

    // Fails at startup on a bad entry: silently dropping it would quietly stop trusting the proxy.
    private static List<IPNetwork> ParseNetworks(IEnumerable<string> cidrs, string configKey)
    {
        List<IPNetwork> networks = [];
        foreach (string cidr in cidrs)
        {
            if (!IPNetwork.TryParse(cidr, out IPNetwork network))
                throw new InvalidOperationException(
                    $"{configKey} contains '{cidr}', which is not a valid CIDR network (e.g. 172.18.0.0/16).");
            networks.Add(network);
        }
        return networks;
    }

    public static IServiceCollection AddSaffaSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        string[]? configuredRanges = configuration
            .GetSection("ForwardedHeaders:TrustedNetworks")
            .Get<string[]>();

        bool hasConfigOverride = configuredRanges is { Length: > 0 };
        string[] seedRanges = hasConfigOverride ? configuredRanges! : CloudflareFallbackCidrs;

        string[]? configuredLocal = configuration
            .GetSection("ForwardedHeaders:LocalProxyNetworks")
            .Get<string[]>();
        string[] localRanges = configuredLocal is { Length: > 0 } ? configuredLocal : DefaultLocalProxyCidrs;
        List<IPNetwork> localNetworks = ParseNetworks(localRanges, "ForwardedHeaders:LocalProxyNetworks");
        List<IPNetwork> seedNetworks = ParseNetworks(seedRanges, "ForwardedHeaders:TrustedNetworks");

        // Local hops (e.g. a cloudflared tunnel container) are always trusted and survive
        // CloudflareIpService refreshes.
        services.AddSingleton(new TrustedProxyNetworks(localNetworks));

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Only the right-most X-Forwarded-For entry: the one Cloudflare's edge appends. Anything
            // left of it came from the visitor. A higher limit would let a visitor whose own IP is in
            // a Cloudflare range (e.g. a Worker) make the next, forged entry count as the client.
            options.ForwardLimit = 1;

            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (IPNetwork network in seedNetworks.Concat(localNetworks).Distinct())
                options.KnownIPNetworks.Add(network);
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
