using Scalar.AspNetCore;

namespace SaffaApi.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddSaffaApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(options =>
        {
            // Behind Cloudflare the app only sees plain HTTP, so the generated absolute server URL
            // can end up as http:// and browsers block Scalar calls from the https page.
            // With no servers listed, clients resolve paths against the origin serving the doc.
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Servers?.Clear();
                return Task.CompletedTask;
            });
        });
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