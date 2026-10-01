using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IVocabularySuggestionService
{
    Task<WordRelatedSuggestionsDto> GetSuggestionsAsync(Guid wordId, CancellationToken cancellationToken = default);
    WordRelatedSuggestionsDto GetSuggestions(WordDto targetWord, IEnumerable<WordDto> vocabularyPool);
    IReadOnlyList<WordSuggestionItemDto> GetSynonymsAndAntonyms(WordDto targetWord, IEnumerable<WordDto> pool);
    IReadOnlyList<WordSuggestionItemDto> GetWordFamily(WordDto targetWord, IEnumerable<WordDto> pool);
    IReadOnlyList<WordSuggestionItemDto> GetCollocationsAndPhrases(WordDto targetWord, IEnumerable<WordDto> pool);
    IReadOnlyList<WordSuggestionItemDto> GetSameTopicWords(WordDto targetWord, IEnumerable<WordDto> pool);
}
