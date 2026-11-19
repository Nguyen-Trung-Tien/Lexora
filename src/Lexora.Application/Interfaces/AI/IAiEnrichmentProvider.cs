namespace Lexora.Application.Interfaces.AI;

public record AiExampleResult(string SentenceEn, string SentenceVi, string? RegisterNote = null);

public record AiGrammarExplanationResult(string SummaryVi, string SummaryEn, IReadOnlyList<string> CommonMistakes);

/// <summary>
/// Optional plugin/provider hook for future Local LLM (Ollama) or BYOK cloud AI enrichment.
/// Lexora remains 100% offline-first; the default implementation is NullAiEnrichmentProvider.
/// </summary>
public interface IAiEnrichmentProvider
{
    string ProviderName { get; }
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiExampleResult>> GenerateContextualExamplesAsync(
        string term,
        string? cefrLevel = null,
        string? examTarget = null,
        int count = 2,
        CancellationToken cancellationToken = default);
    Task<AiGrammarExplanationResult?> ExplainGrammarNuanceAsync(
        string ruleTitle,
        string structure,
        CancellationToken cancellationToken = default);
}
