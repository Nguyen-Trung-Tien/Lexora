using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Application.Interfaces;

public interface IVocabularyService
{
    Task<WordDto?> GetWordByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WordDto>> GetWordsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WordDto>> SearchWordsAsync(
        string? search = null,
        CefrLevel? level = null,
        PartOfSpeech? pos = null,
        Guid? topicId = null,
        ExamType? examType = null,
        LearningStatus? status = null,
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        bool isAscending = true,
        bool? isDueToday = null,
        bool? isWeak = null,
        CancellationToken cancellationToken = default);

    Task<int> CountWordsAsync(
        string? search = null,
        CefrLevel? level = null,
        PartOfSpeech? pos = null,
        Guid? topicId = null,
        ExamType? examType = null,
        LearningStatus? status = null,
        bool? isDueToday = null,
        bool? isWeak = null,
        CancellationToken cancellationToken = default);

    Task<WordDto> CreateWordAsync(WordCreateOrUpdateDto dto, CancellationToken cancellationToken = default);
    Task<WordDto> UpdateWordAsync(WordCreateOrUpdateDto dto, CancellationToken cancellationToken = default);
    Task DeleteWordAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> DeleteWordsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopicDto>> GetAllTopicsAsync(CancellationToken cancellationToken = default);
    Task<TopicDto> CreateTopicAsync(TopicCreateOrUpdateDto dto, CancellationToken cancellationToken = default);
    Task DeleteTopicAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamTagDto>> GetAllExamTagsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContextUnitDto>> GetContextUnitsAsync(CancellationToken cancellationToken = default);
    Task<WordRelatedSuggestionsDto> GetWordSuggestionsAsync(Guid wordId, CancellationToken cancellationToken = default);
    Task<SmartSearchResultPageDto<WordDto>> SmartSearchWordsAsync(
        string? search = null,
        CefrLevel? level = null,
        PartOfSpeech? pos = null,
        Guid? topicId = null,
        ExamType? examType = null,
        LearningStatus? status = null,
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        bool isAscending = true,
        bool? isDueToday = null,
        bool? isWeak = null,
        CancellationToken cancellationToken = default);
}
