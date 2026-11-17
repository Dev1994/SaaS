using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SaffaApi.Services;

namespace SaffaApi.Extensions;

public static class OpenTelemetryExtensions
{
    // Container healthchecks hit this every 30s; keep them out of the trace store.
    private const string HealthPath = "/health";

    /// <summary>
    ///     Traces, metrics and logs exported over OTLP.
    ///     Dev: Aspire (AppHost) injects OTEL_EXPORTER_OTLP_ENDPOINT.
    ///     Prod: OTEL_EXPORTER_OTLP_ENDPOINT points at the self-hosted collector
    ///     (Docker-Raspberry-PI-Monitoring), which forwards metrics to Prometheus and traces to Tempo.
    ///     Without the variable nothing is exported (tests, plain <c>dotnet run</c>).
    /// </summary>
    public static IServiceCollection AddSaffaOpenTelemetry(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        OpenTelemetryBuilder otelBuilder = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: configuration["OpenTelemetry:ServiceName"] ?? "SaffaApi",
                    serviceVersion: configuration["OpenTelemetry:ServiceVersion"])
                .AddAttributes([
                    new KeyValuePair<string, object>("deployment.environment.name", environment.EnvironmentName)
                ]))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = context => !context.Request.Path.StartsWithSegments(HealthPath);
                    options.RecordException = true;
                    // Runs when the request ends, so routing has picked the endpoint and
                    // UseForwardedHeaders has run. The client IP is the real one only when the
                    // tunnel hop is a trusted proxy (ForwardedHeaders config); otherwise it is the
                    // tunnel container's address.
                    options.EnrichWithHttpResponse = (activity, response) => EnrichServerSpan(activity, response.HttpContext);
                })
                .AddHttpClientInstrumentation()
                .AddSource(PhraseService.ActivitySourceName))
            .WithMetrics(metrics => metrics
                // Attach trace IDs (exemplars) to histogram samples recorded inside a sampled trace,
                // so Grafana can jump from a latency spike straight to the Tempo trace behind it.
                .SetExemplarFilter(ExemplarFilterType.TraceBased)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithLogging(_ => { }, options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
            });

        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            otelBuilder.UseOtlpExporter();

        return services;
    }

    public const string RequestIdHeader = "X-Request-Id";

    /// <summary>
    ///     Returns the request ID (<see cref="HttpContext.TraceIdentifier" />, also the RequestId log scope)
    ///     in an <c>X-Request-Id</c> response header, so a caller can quote it and the matching trace
    ///     can be found in Tempo with <c>{ span.saffa.request_id = "..." }</c>.
    /// </summary>
    public static IApplicationBuilder UseSaffaRequestId(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            context.Response.Headers[RequestIdHeader] = context.TraceIdentifier;
            return next(context);
        });

    /// <summary>
    ///     Adds the SaffaApi attributes to the ASP.NET Core server span:
    ///     <c>saffa.operation</c> (stable name per endpoint), <c>saffa.success</c> (status below 400)
    ///     and <c>saffa.request_id</c> (the ID returned in the <c>X-Request-Id</c> header).
    /// </summary>
    internal static void EnrichServerSpan(Activity activity, HttpContext context)
    {
        string? routePattern = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;

        activity.SetTag("saffa.operation", GetOperationName(routePattern));
        activity.SetTag("saffa.success", context.Response.StatusCode < 400);
        activity.SetTag("saffa.request_id", context.TraceIdentifier);
        activity.SetTag("http.client_ip", context.Connection.RemoteIpAddress?.ToString());
        activity.SetTag("http.response_content_length", context.Response.ContentLength);
    }

    /// <summary>
    ///     Maps the matched route template (not the raw path) to an operation name, so values stay
    ///     bounded no matter which terms or scanner paths are requested. Unmatched requests (404)
    ///     have no endpoint and become <c>unknown_operation</c>.
    /// </summary>
    internal static string GetOperationName(string? routePattern) =>
        routePattern?.TrimStart('/') switch
        {
            null => "unknown_operation",
            "" => "get_root",
            "phrase" => "get_random_phrase",
            "phrase/dutch" => "get_dutch_phrase",
            "phrase/{term}" => "get_phrase_by_term",
            "phrase/category/{category}" => "get_phrases_by_category",
            "health" => "health_check",
            var p when p.StartsWith("openapi/", StringComparison.Ordinal) => "get_openapi_document",
            var p when p.StartsWith("scalar", StringComparison.Ordinal) => "get_api_reference",
            _ => "unknown_operation"
        };
}
