var builder = DistributedApplication.CreateBuilder(args);

var otel = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib", "0.114.0")
    .WithHttpEndpoint(4317, 4317, "otlp-grpc")
    .WithHttpEndpoint(8889, 8889, "prometheus")
    .WithBindMount(Path.Combine(builder.Environment.ContentRootPath, "prometheus/otel-config.yml"),
        "/etc/otelcol-contrib/otel-collector-config.yml")
    .WithArgs("--config=/etc/otelcol-contrib/otel-collector-config.yml");

var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one", "latest")
    .WithHttpEndpoint(16686, 16686, "jaeger-ui")
    .WithHttpEndpoint(14250, 14250, "jaeger-grpc")
    .WithHttpEndpoint(14268, 14268, "jaeger-http")
    .WithHttpEndpoint(9411, 9411, "jaeger-zipkin")
    .WithEnvironment("COLLECTOR_OTLP_ENABLED", "true")
    .WithEnvironment("COLLECTOR_ZIPKIN_HOST_PORT", ":9411");

var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.0.1")
    .WithHttpEndpoint(9090, 9090, "prometheus-ui")
    .WithBindMount(Path.Combine(builder.Environment.ContentRootPath, "prometheus/prometheus.yml"),
        "/etc/prometheus/prometheus.yml")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
        "--storage.tsdb.path=/prometheus",
        "--web.console.libraries=/etc/prometheus/console_libraries",
        "--web.console.templates=/etc/prometheus/consoles",
        "--web.enable-lifecycle");

var grafana = builder.AddContainer("grafana", "grafana/grafana", "11.4.0")
    .WithHttpEndpoint(3000, 3000, "grafana-ui")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WithEnvironment("GF_PATHS_PROVISIONING", "/etc/grafana/provisioning")
    .WithBindMount(Path.Combine(builder.Environment.ContentRootPath, "grafana/grafana.ini"),
        "/etc/grafana/grafana.ini")
    .WithBindMount(Path.Combine(builder.Environment.ContentRootPath, "grafana/grafana-datasources.yml"),
        "/etc/grafana/provisioning/datasources/datasources.yml")
    .WithBindMount(Path.Combine(builder.Environment.ContentRootPath, "grafana/dashboards/dashboard-provider.yml"),
        "/etc/grafana/provisioning/dashboards/dashboards.yml")
    .WithBindMount(Path.Combine(builder.Environment.ContentRootPath, "grafana/dashboards"),
        "/var/lib/grafana/dashboards");

var api = builder.AddProject<Projects.SaffaApi>("saffa-webapi")
    .WithEnvironment("OpenTelemetry:ServiceName", "SaffaApi")
    .WithEnvironment("OpenTelemetry:ServiceVersion", "1.0.0")
    .WithEnvironment("OpenTelemetry:OtlpEndpoint", "http://localhost:4317")
    .WithEnvironment("OpenTelemetry:JaegerEndpoint", "http://localhost:14268/api/traces")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WithEnvironment("OTEL_RESOURCE_ATTRIBUTES", "service.name=SaffaApi,service.version=1.0.0")
    .WithEnvironment("OTEL_LOG_LEVEL", "debug")
    .WithEnvironment("JAEGER_ENDPOINT", "http://localhost:14268/api/traces");

builder.Build().Run();