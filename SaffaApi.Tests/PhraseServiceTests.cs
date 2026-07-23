using Xunit;
using SaffaApi.Services;

namespace SaffaApi.Tests;

public class PhraseServiceTests
{
    private static PhraseService CreateService()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "test-phrases.json");
        return new PhraseService(path);
    }

    [Fact]
    public void GetRandom_returns_a_phrase_from_the_set()
    {
        var service = CreateService();

        var phrase = service.GetRandom();

        Assert.False(string.IsNullOrWhiteSpace(phrase.Text));
    }

    [Theory]
    [InlineData("slang", 2)]
    [InlineData("cultural", 1)]
    [InlineData("expression", 1)]
    public void GetByCategory_returns_expected_count(string category, int expected)
    {
        var service = CreateService();

        var phrases = service.GetByCategory(category);

        Assert.Equal(expected, phrases.Count);
    }

    [Fact]
    public void GetByCategory_is_case_insensitive()
    {
        var service = CreateService();

        Assert.Equal(2, service.GetByCategory("SLANG").Count);
    }

    [Fact]
    public void GetByCategory_unknown_returns_empty_list()
    {
        var service = CreateService();

        Assert.Empty(service.GetByCategory("nonexistent"));
    }

    [Fact]
    public void GetByCategory_returns_a_copy_not_internal_list()
    {
        var service = CreateService();

        var first = service.GetByCategory("slang");
        first.Clear();

        Assert.Equal(2, service.GetByCategory("slang").Count);
    }

    [Theory]
    [InlineData("Braai")]
    [InlineData("braai")]
    [InlineData("BRAAI")]
    public void GetByTerm_is_case_insensitive_and_found(string term)
    {
        var service = CreateService();

        var phrase = service.GetByTerm(term);

        Assert.NotNull(phrase);
        Assert.Equal("Braai", phrase!.Text);
    }

    [Fact]
    public void GetByTerm_unknown_returns_null()
    {
        var service = CreateService();

        Assert.Null(service.GetByTerm("notarealterm"));
    }

    [Fact]
    public void GetForDutch_returns_only_phrases_with_dutch_explanation()
    {
        var service = CreateService();

        var dutch = service.GetForDutch();

        Assert.Equal(3, dutch.Count);
        Assert.All(dutch, p => Assert.False(string.IsNullOrWhiteSpace(p.ExplainLikeImDutch)));
    }

    [Fact]
    public void GetRandomForDutch_returns_phrase_with_dutch_explanation()
    {
        var service = CreateService();

        var phrase = service.GetRandomForDutch();

        Assert.False(string.IsNullOrWhiteSpace(phrase.ExplainLikeImDutch));
    }

    [Fact]
    public void Constructor_throws_when_file_missing()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "does-not-exist.json");

        Assert.Throws<FileNotFoundException>(() => new PhraseService(path));
    }

    [Fact]
    public void GetRandom_on_empty_dataset_returns_blank_phrase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"empty-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "[]");
        try
        {
            var service = new PhraseService(path);

            var phrase = service.GetRandom();

            Assert.Equal(string.Empty, phrase.Text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetRandomForDutch_on_empty_dataset_returns_blank_phrase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"empty-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "[]");
        try
        {
            var service = new PhraseService(path);

            var phrase = service.GetRandomForDutch();

            Assert.Equal(string.Empty, phrase.Text);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
