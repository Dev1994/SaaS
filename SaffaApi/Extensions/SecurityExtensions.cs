using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace SaffaApi.Extensions;

public static class SecurityExtensions
{
    private const string CloudflareIpsApi = "https://api.cloudflare.com/client/v4/ips";

    // Cloudflare published proxy ranges. Used as a fallback when the live list cannot
    // be fetched and no ranges are configured. Source: https://www.cloudflare.com/ips/
    private static readonly string[] CloudflareFallbackNetworks =
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
        // Resolve the trusted upstream proxy CIDR ranges. Only X-Forwarded-* headers from
        // these networks are honoured; trusting all sources would let any client spoof
        // X-Forwarded-For and bypass IP-based rate limiting / poison traces.
        string[] trustedNetworks = ResolveTrustedNetworks(configuration);

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

    private static string[] ResolveTrustedNetworks(IConfiguration configuration)
    {
        // 1. Explicit configuration always wins (operator override / non-Cloudflare upstreams).
        if (configuration.GetSection("ForwardedHeaders:TrustedNetworks").Get<string[]>() is { Length: > 0 } configured)
        {
            return configured;
        }

        // 2. Try the live Cloudflare list so ranges stay current without a redeploy.
        if (TryFetchCloudflareNetworks(out string[] live))
        {
            return live;
        }

        // 3. Fall back to the pinned ranges if the fetch fails (offline / API change).
        Console.WriteLine("⚠️  Could not fetch Cloudflare IP list; using pinned fallback ranges.");
        return CloudflareFallbackNetworks;
    }

    private static bool TryFetchCloudflareNetworks(out string[] networks)
    {
        networks = [];
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            string json = http.GetStringAsync(CloudflareIpsApi).GetAwaiter().GetResult();

            using JsonDocument doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("result", out JsonElement result))
            {
                return false;
            }

            List<string> cidrs = [];
            foreach (string prop in new[] { "ipv4_cidrs", "ipv6_cidrs" })
            {
                if (result.TryGetProperty(prop, out JsonElement arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in arr.EnumerateArray())
                    {
                        if (item.GetString() is { Length: > 0 } cidr)
                        {
                            cidrs.Add(cidr);
                        }
                    }
                }
            }

            if (cidrs.Count == 0)
            {
                return false;
            }

            networks = [.. cidrs];
            return true;
        }
        catch
        {
            return false;
        }
    }
}
