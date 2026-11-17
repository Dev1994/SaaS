using IPNetwork = System.Net.IPNetwork;

namespace SaffaApi.Services;

/// <summary>
/// Proxy networks that are always trusted for X-Forwarded-* headers, independent of the
/// Cloudflare edge ranges (e.g. the Docker network a cloudflared tunnel runs on).
/// </summary>
public sealed record TrustedProxyNetworks(IReadOnlyList<IPNetwork> Networks);
