using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using SaffaApi.Services;
using Xunit;
using IPNetwork = System.Net.IPNetwork;

namespace SaffaApi.Tests;

public class ForwardedHeadersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ForwardedHeadersTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task OpenApi_document_does_not_advertise_absolute_server_url()
    {
        var client = _factory.CreateClient();

        var json = await client.GetStringAsync("/openapi/v1.json");

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("servers", out var servers))
        {
            foreach (var server in servers.EnumerateArray())
            {
                var url = server.GetProperty("url").GetString();
                Assert.False(Uri.TryCreate(url, UriKind.Absolute, out _),
                    $"Expected relative server URL, got '{url}'");
            }
        }
    }

    [Fact]
    public void Default_options_trust_local_docker_proxy_networks()
    {
        var options = _factory.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.Contains(options.KnownIPNetworks, n => n.Contains(IPAddress.Parse("172.20.0.2")));
        Assert.Contains(options.KnownIPNetworks, n => n.Contains(IPAddress.Loopback));
        Assert.Contains(options.KnownIPNetworks, n => n.Contains(IPAddress.Parse("104.16.0.1")));
    }

    [Fact]
    public async Task Cloudflare_refresh_keeps_local_proxy_networks()
    {
        var local = IPNetwork.Parse("172.16.0.0/12");
        var stale = IPNetwork.Parse("198.51.100.0/24");
        var options = new ForwardedHeadersOptions();
        options.KnownIPNetworks.Clear();
        options.KnownIPNetworks.Add(local);
        options.KnownIPNetworks.Add(stale);

        const string payload = """
            {"result":{"ipv4_cidrs":["104.16.0.0/13"],"ipv6_cidrs":["2606:4700::/32"]}}
            """;
        var service = new CloudflareIpService(
            Options.Create(options),
            new StubHttpClientFactory(payload),
            new TrustedProxyNetworks([local]),
            NullLogger<CloudflareIpService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
        service.Dispose();

        Assert.Contains(local, options.KnownIPNetworks);
        Assert.Contains(IPNetwork.Parse("104.16.0.0/13"), options.KnownIPNetworks);
        Assert.DoesNotContain(stale, options.KnownIPNetworks);
    }

    // Production: the cloudflared container (Docker network, 172.16.0.0/12) connects to Kestrel,
    // which reports IPv4 peers as IPv4-mapped IPv6 (e.g. ::ffff:172.27.0.1). Cloudflare puts the
    // visitor's IP last in X-Forwarded-For, after anything the visitor sent themselves.
    [Theory]
    [InlineData("::ffff:172.27.0.1", "203.0.113.7", "203.0.113.7")]
    [InlineData("172.27.0.1", "203.0.113.7", "203.0.113.7")]
    [InlineData("::ffff:172.27.0.1", "1.2.3.4, 203.0.113.7", "203.0.113.7")]
    // Visitor sending from Cloudflare space (e.g. a Worker) cannot make its forged entry count.
    [InlineData("::ffff:172.27.0.1", "1.2.3.4, 104.16.0.1", "104.16.0.1")]
    // Direct-to-origin through Cloudflare's proxy (no tunnel): the peer is a Cloudflare edge IP.
    [InlineData("104.16.0.5", "1.2.3.4, 203.0.113.7", "203.0.113.7")]
    [InlineData("8.8.8.8", "203.0.113.7", "8.8.8.8")]
    public async Task Client_ip_comes_from_x_forwarded_for_only_behind_a_trusted_proxy(
        string peer, string forwardedFor, string expectedClientIp)
    {
        List<Activity> exported = [];
        await using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new RemoteIpStartupFilter(IPAddress.Parse(peer)));
                services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddInMemoryExporter(exported));
            }));

        // Other test classes run servers in parallel and their spans reach this listener too.
        string marker = "forwarded-test-" + Guid.NewGuid();
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(marker);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);
        await client.GetAsync("/phrase");
        await factory.DisposeAsync();

        Activity span = Assert.Single(exported, a =>
            a.Kind == ActivityKind.Server && a.GetTagItem("user_agent.original") as string == marker);
        Assert.Equal(IPAddress.Parse(expectedClientIp), IPAddress.Parse((string)span.GetTagItem("http.client_ip")!).MapToIPv4());
    }

    [Theory]
    [InlineData("172.18.0.0/40")]
    [InlineData("not-a-network")]
    public void Invalid_local_proxy_network_fails_at_startup(string cidr)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("ForwardedHeaders:LocalProxyNetworks:0", cidr));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains(cidr, ex.Message);
    }

    private sealed class RemoteIpStartupFilter(IPAddress remoteIp) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = remoteIp;
                return nextMiddleware(context);
            });
            next(app);
        };
    }

    private sealed class StubHttpClientFactory(string payload) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(payload));
    }

    private sealed class StubHandler(string payload) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(payload) });
    }
}
