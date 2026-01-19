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
        string otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"] ??
                              Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ??
                              "http://localhost:4317";
        
        string jaegerEndpoint = configuration["OpenTelemetry:JaegerEndpoint"] ??
                                Environment.GetEnvironmentVariable("JAEGER_ENDPOINT") ??
                                "http://localhost:14268/api/traces";

        Console.WriteLine($"🔍 OpenTelemetry Configuration:");
        Console.WriteLine($"   - Jaeger Endpoint: {jaegerEndpoint}");
        Console.WriteLine($"   - OTLP Endpoint (metrics only): {otlpEndpoint}");

        // Create activity source and meters
        ActivitySource activitySource = new("SaffaApi.Activities");
        Meter customMeter = new("SaffaApi.Metrics");
        Counter<long> requestCounter = customMeter.CreateCounter<long>(
            name: "http_requests_total",
            description: "Total number of HTTP requests by route and status");

        Histogram<double> requestDuration = customMeter.CreateHistogram<double>(
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

            // 🔍 TRACING: Only using Jaeger for distributed tracing
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                            activity.SetTag("http.user_agent", request.Headers.UserAgent.ToString());
                            activity.SetTag("saffa.request_id", Guid.NewGuid().ToString());
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response_content_length", response.ContentLength);
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource(activitySource.Name)
                    .AddSource("SaffaApi.PhraseService") // Add PhraseService activity source
                    .AddJaegerExporter(jaegerOptions =>
                    {
                        Uri uri = new(jaegerEndpoint);
                        jaegerOptions.Endpoint = uri;
                        jaegerOptions.Protocol = JaegerExportProtocol.HttpBinaryThrift;
                        Console.WriteLine($"🔍 Jaeger Tracing configured: {jaegerOptions.Endpoint}");
                    });
            })

            // 📊 METRICS: Using OTLP for metrics collection  
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
                    Console.WriteLine($"📊 Metrics configured: {options.Endpoint}");
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
            ActivitySource activitySource = context.RequestServices.GetRequiredService<ActivitySource>();
            Counter<long> requestCounter = context.RequestServices.GetRequiredService<Counter<long>>();
            Histogram<double> requestDuration = context.RequestServices.GetRequiredService<Histogram<double>>();

            Stopwatch stopwatch = Stopwatch.StartNew();
            string route = context.Request.Path.Value ?? "unknown";
            string method = context.Request.Method;

            using Activity? activity = activitySource.StartActivity($"{method} {route}");
            activity?.SetTag("http.route", route);
            activity?.SetTag("http.method", method);
            activity?.SetTag("http.scheme", context.Request.Scheme);
            activity?.SetTag("http.host", context.Request.Host.Value);
            activity?.SetTag("saffa.operation", GetOperationName(route));

            try
            {
                await next();

                stopwatch.Stop();
                string statusCode = context.Response.StatusCode.ToString();

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
                activity?.SetTag("saffa.success", true);
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
                activity?.SetTag("saffa.success", false);

                throw;
            }
        });
    }

    private static string GetOperationName(string route)
    {
        return route switch
        {
            "/" => "get_root",
            "/phrase" => "get_random_phrase",
            "/phrase/dutch" => "get_dutch_phrase",
            var path when path.StartsWith("/phrase/category/") => "get_phrases_by_category",
            var path when path.StartsWith("/phrase/") => "get_phrase_by_term",
            "/health" => "health_check",
            _ => "unknown_operation"
        };
    }
}