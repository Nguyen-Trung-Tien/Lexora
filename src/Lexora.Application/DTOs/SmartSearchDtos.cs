using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record SmartSearchMatchResult<T>(
    T Item,
    double RelevanceScore,
    string MatchType,
    string? MatchedField = null,
    string? HighlightSnippet = null
);

public record WordSuggestionItemDto(
    WordDto Word,
    SuggestionCategory Category,
    double RelevanceScore,
    string Reason
)
{
    public string Term => Word?.Term ?? string.Empty;
    public string PartOfSpeech => Word?.PrimaryPartOfSpeech.ToString() ?? string.Empty;
    public string PrimaryDefinition => Word?.PrimaryDefinition ?? string.Empty;
    public string ScoreDisplay => $"{Math.Round(RelevanceScore)}%";
    public string CategoryDisplay => Category switch
    {
        SuggestionCategory.Synonym => "Synonym",
        SuggestionCategory.Antonym => "Antonym",
        SuggestionCategory.WordFamily => "Word Family",
        SuggestionCategory.Collocation => "Collocation",
        SuggestionCategory.SameTopic => "Topic",
        _ => "Related"
    };
}

public record WordRelatedSuggestionsDto(
    Guid SourceWordId,
    string SourceTerm,
    IReadOnlyList<WordSuggestionItemDto> Synonyms,
    IReadOnlyList<WordSuggestionItemDto> Antonyms,
    IReadOnlyList<WordSuggestionItemDto> WordFamily,
    IReadOnlyList<WordSuggestionItemDto> Collocations,
    IReadOnlyList<WordSuggestionItemDto> SameTopic
)
{
    public IReadOnlyList<WordSuggestionItemDto> AllSuggestions =>
        Synonyms.Concat(Antonyms).Concat(WordFamily).Concat(Collocations).Concat(SameTopic)
            .OrderByDescending(s => s.RelevanceScore)
            .ToList();

    public int TotalSuggestionsCount =>
        Synonyms.Count + Antonyms.Count + WordFamily.Count + Collocations.Count + SameTopic.Count;
}

public record GrammarSuggestionItemDto(
    GrammarRuleDto Rule,
    GrammarSuggestionCategory Category,
    double RelevanceScore,
    string Reason
)
{
    public string Title => Rule?.Title ?? string.Empty;
    public string Structure => Rule?.Structure ?? string.Empty;
    public string ExplanationVi => Rule?.ExplanationVi ?? string.Empty;
    public string LevelDisplay => Rule?.Level.ToString() ?? string.Empty;
    public string ScoreDisplay => $"{Math.Round(RelevanceScore)}%";
    public string CategoryDisplay => Category switch
    {
        GrammarSuggestionCategory.SimilarStructure => "Similar",
        GrammarSuggestionCategory.ConfusedGrammar => "Confused Pair",
        GrammarSuggestionCategory.SameTopic => "Topic",
        GrammarSuggestionCategory.RelatedVocabulary => "Vocab Link",
        _ => "Related"
    };
}

public record GrammarRelatedSuggestionsDto(
    Guid SourceRuleId,
    string SourceTitle,
    IReadOnlyList<GrammarSuggestionItemDto> SimilarStructures,
    IReadOnlyList<GrammarSuggestionItemDto> ConfusedGrammar,
    IReadOnlyList<GrammarSuggestionItemDto> SameTopicRules,
    IReadOnlyList<WordSuggestionItemDto> RelatedVocabulary
)
{
    public IReadOnlyList<GrammarSuggestionItemDto> AllSuggestions =>
        SimilarStructures.Concat(ConfusedGrammar).Concat(SameTopicRules)
            .OrderByDescending(s => s.RelevanceScore)
            .ToList();

    public int TotalSuggestionsCount =>
        SimilarStructures.Count + ConfusedGrammar.Count + SameTopicRules.Count + RelatedVocabulary.Count;
}

public record SmartSearchResultPageDto<T>(
    IReadOnlyList<SmartSearchMatchResult<T>> Items,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<string> ClosestSuggestions
);
