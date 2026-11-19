using Lexora.Application.Interfaces.AI;

namespace Lexora.Application.Services.AI;

/// <summary>
/// Default offline-first no-op AI enrichment provider.
/// </summary>
public sealed class NullAiEnrichmentProvider : IAiEnrichmentProvider
{
    public string ProviderName => "Offline (Disabled)";

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<IReadOnlyList<AiExampleResult>> GenerateContextualExamplesAsync(
        string term,
        string? cefrLevel = null,
        string? examTarget = null,
        int count = 2,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AiExampleResult>>(Array.Empty<AiExampleResult>());

    public Task<AiGrammarExplanationResult?> ExplainGrammarNuanceAsync(
        string ruleTitle,
        string structure,
        CancellationToken cancellationToken = default)
        => Task.FromResult<AiGrammarExplanationResult?>(null);
}
