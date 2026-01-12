using System.Text.Json;
using SaffaApi.Models;

namespace SaffaApi.Services;

public class PhraseService : IPhraseService
{
    private readonly List<Phrase> _phrases;
    private readonly List<Phrase> _dutchPhrases;
    private readonly Dictionary<string, List<Phrase>> _phrasesByCategory;
    private readonly Dictionary<string, Phrase> _phrasesByTerm;
    private static readonly Random Random = new();

    public PhraseService(string jsonFilePath)
    {
        if (!File.Exists(jsonFilePath))
            throw new FileNotFoundException($"Phrases file not found: {jsonFilePath}");

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
    }

    /// <summary>
    ///     Get a random phrase.
    /// </summary>
    public Phrase GetRandom()
    {
        if (_phrases.Count == 0) return new Phrase();

        lock (Random)
        {
            return _phrases[Random.Next(_phrases.Count)];
        }
    }

    /// <summary>
    ///     Get phrases by category (e.g., "slang", "cultural", "expression").
    /// </summary>
    public List<Phrase> GetByCategory(string category)
    {
        var key = category.ToLowerInvariant();
        return _phrasesByCategory.TryGetValue(key, out var phrases) 
            ? [..phrases] // Return copy to avoid external mutations
            : [];
    }

    /// <summary>
    ///     Get all phrases with Dutch explanation.
    /// </summary>
    public List<Phrase> GetForDutch()
    {
        return [.._dutchPhrases]; // Return copy to avoid external mutations
    }

    /// <summary>
    ///     Get a random phrase with Dutch explanation.
    /// </summary>
    public Phrase GetRandomForDutch()
    {
        if (_dutchPhrases.Count == 0) return new Phrase();

        lock (Random)
        {
            return _dutchPhrases[Random.Next(_dutchPhrases.Count)];
        }
    }

    /// <summary>
    ///     Get phrase by exact term match.
    /// </summary>
    public Phrase? GetByTerm(string term)
    {
        var key = term.ToLowerInvariant();
        return _phrasesByTerm.GetValueOrDefault(key);
    }
}