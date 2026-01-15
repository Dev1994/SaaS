using SaffaApi.Models;
using SaffaApi.Services;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

var phrasesFile = Path.Combine(builder.Environment.ContentRootPath, "data", "phrases.json");

builder.Services.AddSingleton<IPhraseService>(provider => new PhraseService(phrasesFile));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(60);
});

builder.Services.AddRateLimiter(options =>
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
            "Eish! You're going too fast there, boet! Slow down a bit and try again later. 🐢",
            cancellationToken: token);
    };
});

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors("AllowAllOrigins");

app.UseRateLimiter();

app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Saffa – Saffa as a Service")
        .WithTheme(ScalarTheme.Solarized);
});

app.MapGet("/", () => new
{
    name = "Saffa as a Service",
    status = "Sharp sharp!"
}).RequireRateLimiting("ApiPolicy");

app.MapGet("/phrase", (IPhraseService service) => Results.Ok((object?)service.GetRandom()))
    .WithName("GetRandomPhrase")
    .WithSummary("Get a random South African phrase")
    .WithDescription("Returns a random South African slang term or expression. Sharp sharp!")
    .WithTags("Phrases")
    .RequireRateLimiting("ApiPolicy");

app.MapGet("/phrase/dutch", (IPhraseService service) => Results.Ok((object?)service.GetRandomForDutch()))
    .WithName("GetRandomDutchPhrase")
    .WithSummary("Get a random phrase explained for Dutchies")
    .WithDescription("Returns a random South African phrase with a playful Dutch explanation.")
    .WithTags("Phrases")
    .RequireRateLimiting("ApiPolicy");

app.MapGet("/phrase/{term}", (string term, IPhraseService service) =>
    {
        Phrase? phrase = service.GetByTerm(term);
        return phrase is not null ? Results.Ok(phrase) : Results.NotFound();
    })
    .WithName("GetPhraseByTerm")
    .WithSummary("Get a phrase by exact term")
    .WithTags("Phrases")
    .RequireRateLimiting("ApiPolicy");

app.MapGet("/phrase/category/{category}",
        (string category, IPhraseService service) => Results.Ok((object?)service.GetByCategory(category)))
    .WithName("GetPhrasesByCategory")
    .WithSummary("Get phrases filtered by category (slang, cultural, expression)")
    .WithTags("Phrases")
    .RequireRateLimiting("ApiPolicy");

app.Run();