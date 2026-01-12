using SaffaApi.Models;

namespace SaffaApi.Services;

public interface IPhraseService
{
    /// <summary>
    ///     Get a random phrase.
    /// </summary>
    Phrase GetRandom();

    /// <summary>
    ///     Get phrases by category (e.g., "slang", "cultural", "expression").
    /// </summary>
    List<Phrase> GetByCategory(string category);

    /// <summary>
    ///     Get all phrases with Dutch explanation.
    /// </summary>
    List<Phrase> GetForDutch();

    /// <summary>
    ///     Get a random phrase with Dutch explanation.
    /// </summary>
    Phrase GetRandomForDutch();

    /// <summary>
    ///     Get phrase by exact term match.
    /// </summary>
    Phrase? GetByTerm(string term);
}