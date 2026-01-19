using System.Text.Json;
using SaffaApi.Models;
using System.Diagnostics;

namespace SaffaApi.Services;

public class PhraseService : IPhraseService
{
    private readonly List<Phrase> _phrases;
    private readonly List<Phrase> _dutchPhrases;
    private readonly Dictionary<string, List<Phrase>> _phrasesByCategory;
    private readonly Dictionary<string, Phrase> _phrasesByTerm;
    private static readonly Random Random = new();
    private static readonly ActivitySource ActivitySource = new("SaffaApi.PhraseService");

    public PhraseService(string jsonFilePath)
    {
        using var activity = ActivitySource.StartActivity("PhraseService.Initialize");
        activity?.SetTag("file.path", jsonFilePath);
        
        if (!File.Exists(jsonFilePath))
        {
            activity?.SetTag("error", "file_not_found");
            throw new FileNotFoundException($"Phrases file not found: {jsonFilePath}");
        }

        string jsonString = File.ReadAllText(jsonFilePath);
        
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };
        
        _phrases = JsonSerializer.Deserialize<List<Phrase>>(jsonString, options) ?? [];

        // Pre-compute filtered collections for performance
        _dutchPhrases = _phrases
            .Where(p => !string.IsNullOrWhiteSpace(p.ExplainLikeImDutch))
            .ToList();

        _phrasesByCategory = _phrases
            .GroupBy(p => p.Category.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        _phrasesByTerm = _phrases
            .ToDictionary(p => p.Text.ToLowerInvariant(), p => p, StringComparer.OrdinalIgnoreCase);

        // Add telemetry tags
        activity?.SetTag("phrases.total_count", _phrases.Count);
        activity?.SetTag("phrases.dutch_count", _dutchPhrases.Count);
        activity?.SetTag("phrases.category_count", _phrasesByCategory.Count);
    }

    /// <summary>
    ///     Get a random phrase.
    /// </summary>
    public Phrase GetRandom()
    {
        using var activity = ActivitySource.StartActivity("PhraseService.GetRandom");
        activity?.SetTag("operation.type", "get_random_phrase");
        
        if (_phrases.Count == 0) 
        {
            activity?.SetTag("phrases.available", false);
            return new Phrase();
        }

        lock (Random)
        {
            var phrase = _phrases[Random.Next(_phrases.Count)];
            activity?.SetTag("phrases.available", true);
            activity?.SetTag("phrase.category", phrase.Category);
            activity?.SetTag("phrase.has_dutch_explanation", !string.IsNullOrWhiteSpace(phrase.ExplainLikeImDutch));
            return phrase;
        }
    }

    /// <summary>
    ///     Get phrases by category (e.g., "slang", "cultural", "expression").
    /// </summary>
    public List<Phrase> GetByCategory(string category)
    {
        using var activity = ActivitySource.StartActivity("PhraseService.GetByCategory");
        activity?.SetTag("operation.type", "get_by_category");
        activity?.SetTag("category.requested", category);
        
        var key = category.ToLowerInvariant();
        List<Phrase> phrases;
        
        if (_phrasesByCategory.TryGetValue(key, out var foundPhrases))
        {
            phrases = new List<Phrase>(foundPhrases); // Return copy to avoid external mutations
        }
        else
        {
            phrases = new List<Phrase>();
        }
            
        activity?.SetTag("phrases.found_count", phrases.Count);
        activity?.SetTag("category.exists", phrases.Count > 0);
        
        return phrases;
    }

    /// <summary>
    ///     Get all phrases with Dutch explanation.
    /// </summary>
    public List<Phrase> GetForDutch()
    {
        using var activity = ActivitySource.StartActivity("PhraseService.GetForDutch");
        activity?.SetTag("operation.type", "get_for_dutch");
        activity?.SetTag("phrases.dutch_count", _dutchPhrases.Count);
        
        return new List<Phrase>(_dutchPhrases); // Return copy to avoid external mutations
    }

    /// <summary>
    ///     Get a random phrase with Dutch explanation.
    /// </summary>
    public Phrase GetRandomForDutch()
    {
        using var activity = ActivitySource.StartActivity("PhraseService.GetRandomForDutch");
        activity?.SetTag("operation.type", "get_random_dutch_phrase");
        
        if (_dutchPhrases.Count == 0) 
        {
            activity?.SetTag("phrases.dutch_available", false);
            return new Phrase();
        }

        lock (Random)
        {
            var phrase = _dutchPhrases[Random.Next(_dutchPhrases.Count)];
            activity?.SetTag("phrases.dutch_available", true);
            activity?.SetTag("phrase.category", phrase.Category);
            return phrase;
        }
    }

    /// <summary>
    ///     Get phrase by exact term match.
    /// </summary>
    public Phrase? GetByTerm(string term)
    {
        using var activity = ActivitySource.StartActivity("PhraseService.GetByTerm");
        activity?.SetTag("operation.type", "get_by_term");
        activity?.SetTag("term.requested", term);
        
        var key = term.ToLowerInvariant();
        var phrase = _phrasesByTerm.GetValueOrDefault(key);
        
        activity?.SetTag("phrase.found", phrase is not null);
        if (phrase is not null)
        {
            activity?.SetTag("phrase.category", phrase.Category);
        }
        
        return phrase;
    }
}