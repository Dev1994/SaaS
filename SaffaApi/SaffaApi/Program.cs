using SaffaApi.Models;
using SaffaApi.Services;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Path to your phrases.json
var phrasesFile = Path.Combine(builder.Environment.ContentRootPath, "data", "phrases.json");

// Register as singleton since the data is read-only and immutable
builder.Services.AddSingleton<IPhraseService>(provider => new PhraseService(phrasesFile));

// Add OpenAPI (Swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// OpenAPI endpoint
app.MapOpenApi();

// Scalar UI
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Saffa – South African as a Service")
        .WithTheme(ScalarTheme.Solarized);
});

// Root endpoint
app.MapGet("/", () => new
{
    name = "South African as a Service",
    status = "Sharp sharp!"
});

// Get a random phrase (optionally filter by category or language)
app.MapGet("/phrase", (IPhraseService service) => Results.Ok((object?) service.GetRandom()))
    .WithName("GetRandomPhrase")
    .WithSummary("Get a random South African phrase")
    .WithDescription("Returns a random South African slang term or expression. Sharp sharp!")
    .WithTags("Phrases");

// Get a random Dutch-explained phrase
app.MapGet("/phrase/dutch", (IPhraseService service) => Results.Ok((object?) service.GetRandomForDutch()))
    .WithName("GetRandomDutchPhrase")
    .WithSummary("Get a random phrase explained for Dutchies")
    .WithDescription("Returns a random South African phrase with a playful Dutch explanation.")
    .WithTags("Phrases");

// Get phrase by exact term
app.MapGet("/phrase/{term}", (string term, IPhraseService service) =>
    {
        Phrase? phrase = service.GetByTerm(term);
        return phrase is not null ? Results.Ok(phrase) : Results.NotFound();
    })
    .WithName("GetPhraseByTerm")
    .WithSummary("Get a phrase by exact term")
    .WithTags("Phrases");

// Get phrases by category
app.MapGet("/phrase/category/{category}",
        (string category, IPhraseService service) => Results.Ok((object?) service.GetByCategory(category)))
    .WithName("GetPhrasesByCategory")
    .WithSummary("Get phrases filtered by category (slang, cultural, expression)")
    .WithTags("Phrases");

app.Run();