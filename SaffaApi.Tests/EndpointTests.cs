using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SaffaApi.Models;
using Xunit;

namespace SaffaApi.Tests;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Root_returns_welcome_payload()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Saffa as a Service", body);
    }

    [Fact]
    public async Task Phrase_returns_a_non_empty_phrase()
    {
        var client = _factory.CreateClient();

        var phrase = await client.GetFromJsonAsync<Phrase>("/phrase", JsonOptions);

        Assert.NotNull(phrase);
        Assert.False(string.IsNullOrWhiteSpace(phrase!.Text));
    }

    [Fact]
    public async Task Phrase_dutch_returns_phrase_with_dutch_explanation()
    {
        var client = _factory.CreateClient();

        var phrase = await client.GetFromJsonAsync<Phrase>("/phrase/dutch", JsonOptions);

        Assert.NotNull(phrase);
        Assert.False(string.IsNullOrWhiteSpace(phrase!.ExplainLikeImDutch));
    }

    [Fact]
    public async Task Phrase_by_term_returns_200_for_known_term()
    {
        var client = _factory.CreateClient();

        // Fetch a real phrase first so the test does not hard-code data.
        var seed = await client.GetFromJsonAsync<Phrase>("/phrase", JsonOptions);
        Assert.NotNull(seed);

        var response = await client.GetAsync($"/phrase/{Uri.EscapeDataString(seed!.Text)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var found = await response.Content.ReadFromJsonAsync<Phrase>(JsonOptions);
        Assert.Equal(seed.Text, found!.Text);
    }

    [Fact]
    public async Task Phrase_by_term_returns_404_for_unknown_term()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/phrase/definitely-not-a-real-term-xyz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Phrase_by_category_returns_list()
    {
        var client = _factory.CreateClient();

        var phrases = await client.GetFromJsonAsync<List<Phrase>>("/phrase/category/slang", JsonOptions);

        Assert.NotNull(phrases);
        Assert.NotEmpty(phrases!);
        Assert.All(phrases!, p => Assert.Equal("slang", p.Category, ignoreCase: true));
    }

    [Fact]
    public async Task Phrase_by_unknown_category_returns_empty_list()
    {
        var client = _factory.CreateClient();

        var phrases = await client.GetFromJsonAsync<List<Phrase>>("/phrase/category/nope", JsonOptions);

        Assert.NotNull(phrases);
        Assert.Empty(phrases!);
    }

    [Fact]
    public async Task Health_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
