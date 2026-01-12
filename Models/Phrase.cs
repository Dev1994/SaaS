namespace SaffaApi.Models
{
    /// <summary>
    /// Represents a South African phrase, slang, or cultural expression.
    /// Includes metadata for Dutch explanations and usage context.
    /// </summary>
    public class Phrase
    {
        /// <summary>
        /// The South African phrase itself.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The category of the phrase (e.g., "expression", "slang", "cultural").
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// What the phrase actually means in context.
        /// </summary>
        public string ActualMeaning { get; set; } = string.Empty;

        /// <summary>
        /// Whether the phrase has Afrikaans influence.
        /// </summary>
        public bool AfrikaansInfluence { get; set; }

        /// <summary>
        /// Explanation tailored for Dutch colleagues who might misunderstand the phrase.
        /// </summary>
        public string ExplainLikeImDutch { get; set; } = string.Empty;

        /// <summary>
        /// Probability (0–1) that a Dutch person might misunderstanding the phrase.
        /// </summary>
        public double MisunderstandingProbability { get; set; }

        /// <summary>
        /// Confidence level in the accuracy of this phrase and explanation.
        /// </summary>
        public string Confidence { get; set; } = "High";
    }
}