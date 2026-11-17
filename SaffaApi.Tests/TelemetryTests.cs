using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using SaffaApi.Extensions;
using SaffaApi.Services;
using Xunit;

namespace SaffaApi.Tests;

public class TelemetryTests
{
    [Theory]
    [InlineData("/", "get_root")]
    [InlineData("/phrase", "get_random_phrase")]
    [InlineData("/phrase/dutch", "get_dutch_phrase")]
    [InlineData("/phrase/{term}", "get_phrase_by_term")]
    [InlineData("/phrase/category/{category}", "get_phrases_by_category")]
    [InlineData("/health", "health_check")]
    [InlineData("health", "health_check")]
    [InlineData("/openapi/{documentName}.json", "get_openapi_document")]
    [InlineData("/scalar/{documentName?}", "get_api_reference")]
    [InlineData(null, "unknown_operation")]
    [InlineData("/something/else", "unknown_operation")]
    public void Operation_name_comes_from_route_template(string? routePattern, string expected)
    {
        Assert.Equal(expected, OpenTelemetryExtensions.GetOperationName(routePattern));
    }

    [Fact]
    public async Task Server_spans_carry_saffa_attributes_and_skip_health()
    {
        List<Activity> exported = [];
        await using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddInMemoryExporter(exported))));

        // Other test classes run servers in parallel and their spans reach this listener too,
        // so mark this test's requests and only look at those.
        string marker = "telemetry-test-" + Guid.NewGuid();
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(marker);
        await client.GetAsync("/health");
        HttpResponseMessage byTermResponse = await client.GetAsync("/phrase/this-term-does-not-exist");
        await client.GetAsync("/no/such/route");

        // The provider exports on Activity stop; dispose to flush the simple processor.
        await factory.DisposeAsync();

        List<Activity> serverSpans = exported.Where(a => a.Kind == ActivityKind.Server && a.GetTagItem("user_agent.original") as string == marker).ToList();

        Assert.DoesNotContain(serverSpans, a => a.GetTagItem("url.path") as string == "/health");

        Activity byTerm = Assert.Single(serverSpans, a => a.GetTagItem("http.route") as string == "/phrase/{term}");
        Assert.Equal("get_phrase_by_term", byTerm.GetTagItem("saffa.operation"));
        Assert.Equal(false, byTerm.GetTagItem("saffa.success"));
        // The ID returned to the caller is the one on the span, so a quoted ID finds the trace.
        string requestId = Assert.Single(byTermResponse.Headers.GetValues(OpenTelemetryExtensions.RequestIdHeader));
        Assert.False(string.IsNullOrWhiteSpace(requestId));
        Assert.Equal(requestId, byTerm.GetTagItem("saffa.request_id"));

        Activity unmatched = Assert.Single(serverSpans, a => a.GetTagItem("url.path") as string == "/no/such/route");
        Assert.Equal("unknown_operation", unmatched.GetTagItem("saffa.operation"));

        // One server span per request, and no second request-level span alongside it: the only
        // other spans in these traces are PhraseService operations.
        Assert.Equal(2, serverSpans.Count);
        HashSet<ActivityTraceId> traceIds = serverSpans.Select(a => a.TraceId).ToHashSet();
        Assert.All(
            exported.Where(a => traceIds.Contains(a.TraceId) && a.Kind != ActivityKind.Server),
            a => Assert.Equal(PhraseService.ActivitySourceName, a.Source.Name));
    }
}
