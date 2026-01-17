using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SaffaApi.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddSaffaOpenTelemetry(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Get OpenTelemetry endpoint (configured by Aspire)
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"] ??
                          Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ??
                          "http://localhost:4317";

        Console.WriteLine($"?? OpenTelemetry OTLP Endpoint: {otlpEndpoint}");

        // Create activity source and meters
        var activitySource = new ActivitySource("SaffaApi.Activities");
        var customMeter = new Meter("SaffaApi.Metrics");
        var requestCounter = customMeter.CreateCounter<long>(
            name: "http_requests_total",
            description: "Total number of HTTP requests by route and status");

        var requestDuration = customMeter.CreateHistogram<double>(
            name: "http_request_duration_seconds",
            description: "Duration of HTTP requests by route");

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: configuration["OpenTelemetry:ServiceName"] ?? "SaffaApi",
                    serviceVersion: configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment.EnvironmentName,
                    ["host.name"] = Environment.MachineName,
                    ["service.instance.id"] = Environment.MachineName + "_" + Environment.ProcessId
                }))

            // ?? TRACING: Captures request flows and timing
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                        activity.SetTag("http.user_agent", request.Headers.UserAgent.ToString());
                    };
                    options.EnrichWithHttpResponse = (activity, response) =>
                    {
                        activity.SetTag("http.response_content_length", response.ContentLength);
                    };
                })
                .AddHttpClientInstrumentation()
                .AddSource(activitySource.Name)
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                    options.TimeoutMilliseconds = 10000;
                    Console.WriteLine($"?? Tracing configured: {options.Endpoint}");
                }))

            // ?? METRICS: Captures performance and usage data  
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddMeter(customMeter.Name)
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                    options.TimeoutMilliseconds = 10000;
                    Console.WriteLine($"?? Metrics configured: {options.Endpoint}");
                }));

        // Register telemetry objects as singletons
        services.AddSingleton(activitySource);
        services.AddSingleton(customMeter);
        services.AddSingleton(requestCounter);
        services.AddSingleton(requestDuration);

        return services;
    }

    public static IApplicationBuilder UseSaffaTelemetryMiddleware(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var activitySource = context.RequestServices.GetRequiredService<ActivitySource>();
            var requestCounter = context.RequestServices.GetRequiredService<Counter<long>>();
            var requestDuration = context.RequestServices.GetRequiredService<Histogram<double>>();

            var stopwatch = Stopwatch.StartNew();
            var route = context.Request.Path.Value ?? "unknown";
            var method = context.Request.Method;

            using var activity = activitySource.StartActivity($"{method} {route}");
            activity?.SetTag("http.route", route);
            activity?.SetTag("http.method", method);
            activity?.SetTag("http.scheme", context.Request.Scheme);
            activity?.SetTag("http.host", context.Request.Host.Value);

            try
            {
                await next();

                stopwatch.Stop();
                var statusCode = context.Response.StatusCode.ToString();

                requestCounter.Add(1, new TagList
                {
                    { "route", route },
                    { "method", method },
                    { "status_code", statusCode }
                });

                requestDuration.Record(stopwatch.Elapsed.TotalSeconds, new TagList
                {
                    { "route", route },
                    { "method", method },
                    { "status_code", statusCode }
                });

                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("http.status_code", statusCode);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                requestCounter.Add(1, new TagList
                {
                    { "route", route },
                    { "method", method },
                    { "status_code", "500" }
                });

                requestDuration.Record(stopwatch.Elapsed.TotalSeconds, new TagList
                {
                    { "route", route },
                    { "method", method },
                    { "status_code", "500" }
                });

                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("http.status_code", "500");
                activity?.SetTag("exception.type", ex.GetType().Name);
                activity?.SetTag("exception.message", ex.Message);

                throw;
            }
        });
    }
}