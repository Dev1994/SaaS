using Scalar.AspNetCore;

namespace SaffaApi.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddSaffaApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi();
        services.AddHealthChecks();

        return services;
    }

    public static WebApplication UseSaffaApi(this WebApplication app)
    {
        app.MapOpenApi();

        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("Saffa – Saffa as a Service")
                .WithTheme(ScalarTheme.Solarized);
        });

        return app;
    }
}