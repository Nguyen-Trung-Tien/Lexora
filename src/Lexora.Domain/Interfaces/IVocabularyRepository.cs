using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Interfaces;

public interface IVocabularyRepository : IRepository<Word>
{
    Task<Word?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Word>> GetByIdsWithDetailsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<Word?> GetByTermAsync(string term, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Word>> SearchAsync(
        string? searchTerm = null,
        CefrLevel? level = null,
        PartOfSpeech? partOfSpeech = null,
        Guid? topicId = null,
        ExamType? examType = null,
        LearningStatus? status = null,
        int skip = 0,
        int take = 50,
        string? sortBy = null,
        bool isAscending = true,
        bool? isDueToday = null,
        bool? isWeak = null,
        CancellationToken cancellationToken = default);

    Task<int> CountFilteredAsync(
        string? searchTerm = null,
        CefrLevel? level = null,
        PartOfSpeech? partOfSpeech = null,
        Guid? topicId = null,
        ExamType? examType = null,
        LearningStatus? status = null,
        bool? isDueToday = null,
        bool? isWeak = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Word>> GetRandomWordsAsync(int count, Guid? excludeWordId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Word>> GetWordsForAnalyticsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Topic>> GetAllTopicsAsync(CancellationToken cancellationToken = default);
    Task<Topic?> GetTopicByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Topic?> GetTopicByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddTopicAsync(Topic topic, CancellationToken cancellationToken = default);
    void DeleteTopic(Topic topic);
    Task<IReadOnlyList<ExamTag>> GetAllExamTagsAsync(CancellationToken cancellationToken = default);
    Task AddExamTagAsync(ExamTag examTag, CancellationToken cancellationToken = default);
    void RemoveWordChildren(Word word);
    void AddMeanings(IEnumerable<WordMeaning> meanings);
    void AddWordTopics(IEnumerable<WordTopic> wordTopics);
    void AddWordExamTags(IEnumerable<WordExamTag> wordExamTags);
    Task<HashSet<string>> GetAllNormalizedTermsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, Word>> GetWordsByTermsWithDetailsAsync(IEnumerable<string> normalizedTerms, CancellationToken cancellationToken = default);
}
