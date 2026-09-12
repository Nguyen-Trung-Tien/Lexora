using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Application.Interfaces;

public interface IVocabularyService
{
    Task<WordDto?> GetWordByIdAsync(Guid id, CancellationToken cancellationToken = default);
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
        CancellationToken cancellationToken = default);

    Task<int> CountWordsAsync(
        string? search = null,
        CefrLevel? level = null,
        PartOfSpeech? pos = null,
        Guid? topicId = null,
        ExamType? examType = null,
        LearningStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<WordDto> CreateWordAsync(WordCreateOrUpdateDto dto, CancellationToken cancellationToken = default);
    Task<WordDto> UpdateWordAsync(WordCreateOrUpdateDto dto, CancellationToken cancellationToken = default);
    Task DeleteWordAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopicDto>> GetAllTopicsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamTagDto>> GetAllExamTagsAsync(CancellationToken cancellationToken = default);
}
