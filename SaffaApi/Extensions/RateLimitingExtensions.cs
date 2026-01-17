using System.Threading.RateLimiting;

namespace SaffaApi.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddSaffaRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("ApiPolicy", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.Get($"minute_{ipAddress}", partition =>
                    new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 100,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 100,
                        AutoReplenishment = true
                    }));
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"hourly_{ipAddress}",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 500,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsync(
                    "Eish! You're going too fast there, boet! Slow down a bit and try again later. ??",
                    cancellationToken: token);
            };
        });

        return services;
    }
}