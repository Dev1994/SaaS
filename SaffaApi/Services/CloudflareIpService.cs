using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using IPNetwork = System.Net.IPNetwork;

namespace SaffaApi.Services;

public sealed class CloudflareIpService(
    IOptions<ForwardedHeadersOptions> forwardedHeadersOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<CloudflareIpService> logger) : IHostedService, IDisposable
{
    private const string CloudflareIpsApi = "https://api.cloudflare.com/client/v4/ips";
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(24);

    private readonly ForwardedHeadersOptions _options = forwardedHeadersOptions.Value;
    private readonly object _lock = new();
    private PeriodicTimer? _timer;
    private Task? _timerTask;
    private CancellationTokenSource? _cts;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Fetch immediately so ranges are live before the first request hits.
        await RefreshAsync(cancellationToken);

        // Then keep refreshing in the background every 24 hours.
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timer = new PeriodicTimer(RefreshInterval);
        _timerTask = RunTimerAsync(_cts.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();

        if (_timerTask is not null)
            await _timerTask.WaitAsync(cancellationToken);
    }

    private async Task RunTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_timer is not null && await _timer.WaitForNextTickAsync(cancellationToken))
            {
                await RefreshAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — nothing to do.
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        List<IPNetwork>? ranges = await FetchCloudflareRangesAsync(cancellationToken);

        if (ranges is null || ranges.Count == 0)
        {
            logger.LogWarning("Cloudflare IP refresh failed — keeping existing ranges.");
            return;
        }

        lock (_lock)
        {
            // ForwardedHeadersMiddleware reads KnownIPNetworks per-request without this lock.
            // KnownIPNetworks is a getter-only List<T> — we cannot swap it for a thread-safe
            // collection. We minimise the mutation window by adding new ranges before removing
            // stale ones, so there is never an empty-list gap that would reject all requests.
            // At a 24-hour refresh cadence the overlap with a live request is negligible.
            foreach (IPNetwork network in ranges)
            {
                if (!_options.KnownIPNetworks.Contains(network))
                    _options.KnownIPNetworks.Add(network);
            }

            List<IPNetwork> stale = _options.KnownIPNetworks.Except(ranges).ToList();
            foreach (IPNetwork network in stale)
                _options.KnownIPNetworks.Remove(network);
        }

        logger.LogInformation("Cloudflare IP ranges refreshed — {Count} networks loaded.", ranges.Count);
    }

    private async Task<List<IPNetwork>?> FetchCloudflareRangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            HttpClient http = httpClientFactory.CreateClient("cloudflare");
            string json = await http.GetStringAsync(CloudflareIpsApi, cancellationToken);

            using JsonDocument doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("result", out JsonElement result))
            {
                logger.LogWarning("Cloudflare IP API response missing 'result' field.");
                return null;
            }

            List<IPNetwork> parsed = [];

            foreach (string prop in new[] { "ipv4_cidrs", "ipv6_cidrs" })
            {
                if (!result.TryGetProperty(prop, out JsonElement arr) || arr.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (JsonElement item in arr.EnumerateArray())
                {
                    string? cidr = item.GetString();
                    if (string.IsNullOrEmpty(cidr))
                        continue;

                    if (IPNetwork.TryParse(cidr, out IPNetwork network))
                        parsed.Add(network);
                    else
                        logger.LogWarning("Could not parse CIDR '{Cidr}' from Cloudflare API — skipping.", cidr);
                }
            }

            return parsed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to fetch Cloudflare IP ranges.");
            return null;
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _cts?.Dispose();
    }
}
