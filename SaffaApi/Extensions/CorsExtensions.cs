namespace SaffaApi.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddSaffaCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      // Let browser clients read the request ID to quote it in bug reports.
                      .WithExposedHeaders(OpenTelemetryExtensions.RequestIdHeader);
            });
        });

        return services;
    }
}