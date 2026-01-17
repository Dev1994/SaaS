using SaffaApi.Extensions;
using SaffaApi.Models;
using SaffaApi.Services;

var builder = WebApplication.CreateBuilder(args);

 var phrasesFile = Path.Combine(builder.Environment.ContentRootPath, "data", "phrases.json");
 builder.Services.AddSingleton<IPhraseService>(provider => new PhraseService(phrasesFile));

builder.Services.AddSaffaApi();
builder.Services.AddSaffaCors();
builder.Services.AddSaffaSecurity();
builder.Services.AddSaffaRateLimiting();
builder.Services.AddSaffaOpenTelemetry(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseSaffaTelemetryMiddleware();
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors("AllowAllOrigins");
app.UseRateLimiter();
app.UseSaffaApi();

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

app.MapHealthChecks("/health");

app.Run();