using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class VocabularyService : IVocabularyService
{
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISpacedRepetitionEngine _srEngine;
    private readonly ISmartSearchService? _smartSearch;
    private readonly ISmartSearchCacheService? _cacheService;

    public VocabularyService(
        IVocabularyRepository vocabRepo,
        IReviewRepository reviewRepo,
        IUnitOfWork unitOfWork,
        ISpacedRepetitionEngine? srEngine = null,
        ISmartSearchService? smartSearch = null,
        ISmartSearchCacheService? cacheService = null)
    {
        _vocabRepo = vocabRepo;
        _reviewRepo = reviewRepo;
        _unitOfWork = unitOfWork;
        _srEngine = srEngine ?? new Sm2SpacedRepetitionEngine();
        _smartSearch = smartSearch;
        _cacheService = cacheService;
    }

    public async Task<WordDto?> GetWordByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var word = await _vocabRepo.GetWithDetailsAsync(id, cancellationToken);
        return word == null ? null : MapToDto(word);
    }

    public async Task<IReadOnlyList<WordDto>> GetWordsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var words = await _vocabRepo.GetByIdsWithDetailsAsync(ids, cancellationToken);
        return words.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<WordDto>> SearchWordsAsync(
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
        CancellationToken cancellationToken = default)
    {
        int skip = Math.Max(0, (page - 1) * pageSize);

        // Smart relevance sorting when requested or searching
        if (sortBy?.Equals("relevance", StringComparison.OrdinalIgnoreCase) == true &&
            !string.IsNullOrWhiteSpace(search) &&
            _smartSearch != null)
        {
            int poolSize = Math.Max(300, page * pageSize * 3);
            IReadOnlyList<Word> rawWords;
            if (isDueToday == null && isWeak == null)
            {
                rawWords = await _vocabRepo.SearchAsync(
                    search, level, pos, topicId, examType, status,
                    0, poolSize, null, true, cancellationToken);
            }
            else
            {
                rawWords = await _vocabRepo.SearchAsync(
                    search, level, pos, topicId, examType, status,
                    0, poolSize, null, true, isDueToday, isWeak, cancellationToken);
            }

            var dtoList = rawWords.Select(MapToDto).ToList();
            var ranked = _smartSearch.RankWords(search, dtoList, dtoList.Count);
            return ranked.Skip(skip).Take(pageSize).Select(r => r.Item).ToList();
        }

        IReadOnlyList<Word> words;
        if (isDueToday == null && isWeak == null)
        {
            words = await _vocabRepo.SearchAsync(
                search, level, pos, topicId, examType, status,
                skip, pageSize, sortBy, isAscending, cancellationToken);
        }
        else
        {
            words = await _vocabRepo.SearchAsync(
                search, level, pos, topicId, examType, status,
                skip, pageSize, sortBy, isAscending, isDueToday, isWeak, cancellationToken);
        }

        return words.Select(MapToDto).ToList();
    }

    public async Task<int> CountWordsAsync(
        string? search = null,
        CefrLevel? level = null,
        PartOfSpeech? pos = null,
        Guid? topicId = null,
        ExamType? examType = null,
        LearningStatus? status = null,
        bool? isDueToday = null,
        bool? isWeak = null,
        CancellationToken cancellationToken = default)
    {
        if (isDueToday == null && isWeak == null)
        {
            return await _vocabRepo.CountFilteredAsync(search, level, pos, topicId, examType, status, cancellationToken);
        }
        else
        {
            return await _vocabRepo.CountFilteredAsync(search, level, pos, topicId, examType, status, isDueToday, isWeak, cancellationToken);
        }
    }

    public async Task<WordDto> CreateWordAsync(WordCreateOrUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var trimmedTerm = dto.Term?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedTerm))
        {
            throw new ArgumentException("Word term cannot be empty.", nameof(dto));
        }

        var existingWord = await _vocabRepo.GetByTermAsync(trimmedTerm, cancellationToken);
        if (existingWord != null)
        {
            throw new InvalidOperationException($"Word '{trimmedTerm}' already exists.");
        }

        var word = new Word
        {
            Term = trimmedTerm,
            NormalizedTerm = trimmedTerm.ToLowerInvariant(),
            PhoneticUk = dto.PhoneticUk,
            PhoneticUs = dto.PhoneticUs,
            AudioUkPath = dto.AudioUkPath,
            AudioUsPath = dto.AudioUsPath,
            Level = dto.Level,
            PrimaryPartOfSpeech = dto.PrimaryPartOfSpeech,
            Notes = dto.Notes
        };

        // Create default meanings
        int idx = 0;
        foreach (var m in dto.Meanings)
        {
            var meaning = new WordMeaning
            {
                WordId = word.Id,
                PartOfSpeech = m.PartOfSpeech,
                DefinitionVi = m.DefinitionVi,
                DefinitionEn = m.DefinitionEn,
                Explanation = m.Explanation,
                OrderIndex = idx++
            };

            foreach (var ex in m.Examples)
            {
                meaning.Examples.Add(new ExampleSentence
                {
                    SentenceEn = ex.SentenceEn,
                    SentenceVi = ex.SentenceVi,
                    AudioPath = ex.AudioPath
                });
            }

            word.Meanings.Add(meaning);
        }

        // Add topic associations
        foreach (var topicId in dto.TopicIds)
        {
            word.WordTopics.Add(new WordTopic { WordId = word.Id, TopicId = topicId });
        }

        // Add exam tag associations
        foreach (var examTagId in dto.ExamTagIds)
        {
            word.WordExamTags.Add(new WordExamTag { WordId = word.Id, ExamTagId = examTagId });
        }

        // Initialize review card for spaced repetition
        var card = new ReviewCard
        {
            WordId = word.Id,
            Status = LearningStatus.New,
            NextReviewAt = DateTime.UtcNow
        };
        word.ReviewCard = card;

        await _vocabRepo.AddAsync(word, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _cacheService?.InvalidateVocabulary();

        // Fetch back with full details
        var savedWord = await _vocabRepo.GetWithDetailsAsync(word.Id, cancellationToken);
        return MapToDto(savedWord ?? word);
    }

    public async Task<WordDto> UpdateWordAsync(WordCreateOrUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Id == null)
            throw new ArgumentException("Word Id must not be null for update", nameof(dto));

        var trimmedTerm = dto.Term?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedTerm))
            throw new ArgumentException("Word term cannot be empty.", nameof(dto));

        var word = await _vocabRepo.GetWithDetailsAsync(dto.Id.Value, cancellationToken);
        if (word == null)
            throw new KeyNotFoundException($"Word with ID {dto.Id} not found");

        var duplicate = await _vocabRepo.GetByTermAsync(trimmedTerm, cancellationToken);
        if (duplicate != null && duplicate.Id != dto.Id.Value)
        {
            throw new InvalidOperationException($"Word '{trimmedTerm}' already exists.");
        }

        word.Term = trimmedTerm;
        word.NormalizedTerm = trimmedTerm.ToLowerInvariant();
        word.PhoneticUk = dto.PhoneticUk;
        word.PhoneticUs = dto.PhoneticUs;
        word.AudioUkPath = dto.AudioUkPath;
        word.AudioUsPath = dto.AudioUsPath;
        word.Level = dto.Level;
        word.PrimaryPartOfSpeech = dto.PrimaryPartOfSpeech;
        word.Notes = dto.Notes;
        word.UpdatedAt = DateTime.UtcNow;

        // Clean up previous children in DB context
        _vocabRepo.RemoveWordChildren(word);

        // Update Meanings
        var newMeanings = new List<WordMeaning>();
        int idx = 0;
        foreach (var m in dto.Meanings)
        {
            var meaning = new WordMeaning
            {
                WordId = word.Id,
                PartOfSpeech = m.PartOfSpeech,
                DefinitionVi = m.DefinitionVi,
                DefinitionEn = m.DefinitionEn,
                Explanation = m.Explanation,
                OrderIndex = idx++
            };

            foreach (var ex in m.Examples)
            {
                meaning.Examples.Add(new ExampleSentence
                {
                    SentenceEn = ex.SentenceEn,
                    SentenceVi = ex.SentenceVi,
                    AudioPath = ex.AudioPath
                });
            }

            newMeanings.Add(meaning);
        }

        foreach (var meaning in newMeanings)
        {
            word.Meanings.Add(meaning);
        }
        _vocabRepo.AddMeanings(newMeanings);

        // Update Topics
        var newTopics = dto.TopicIds.Select(tid => new WordTopic { WordId = word.Id, TopicId = tid }).ToList();
        foreach (var topic in newTopics)
        {
            word.WordTopics.Add(topic);
        }
        _vocabRepo.AddWordTopics(newTopics);

        // Update Exam Tags
        var newTags = dto.ExamTagIds.Select(etid => new WordExamTag { WordId = word.Id, ExamTagId = etid }).ToList();
        foreach (var tag in newTags)
        {
            word.WordExamTags.Add(tag);
        }
        _vocabRepo.AddWordExamTags(newTags);

        await _vocabRepo.UpdateAsync(word, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _cacheService?.InvalidateVocabulary();

        var refreshed = await _vocabRepo.GetByIdsWithDetailsAsync([word.Id], cancellationToken);
        var savedWord = refreshed.FirstOrDefault() ?? await _vocabRepo.GetWithDetailsAsync(word.Id, cancellationToken);
        return MapToDto(savedWord ?? word);
    }

    public async Task DeleteWordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) return;

        var word = await _vocabRepo.GetWithDetailsAsync(id, cancellationToken)
                   ?? await _vocabRepo.GetByIdAsync(id, cancellationToken);
        if (word != null)
        {
            await _vocabRepo.DeleteAsync(word, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _cacheService?.InvalidateVocabulary();
        }
    }

    public async Task<int> DeleteWordsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var idList = ids.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return 0;

        progress?.Report(new ProgressReport(0, 0, idList.Count, "Bắt đầu xóa dữ liệu...", "Preparing deletion..."));

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            int deletedCount = 0;
            int total = idList.Count;
            int chunkIndex = 0;
            var chunks = idList.Chunk(150).ToList();

            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                chunkIndex++;

                var words = await _vocabRepo.GetByIdsWithDetailsAsync(chunk, cancellationToken);
                if (words == null || words.Count == 0)
                {
                    var fallbackWords = new List<Word>();
                    foreach (var id in chunk)
                    {
                        var w = await _vocabRepo.GetWithDetailsAsync(id, cancellationToken)
                                ?? await _vocabRepo.GetByIdAsync(id, cancellationToken);
                        if (w != null) fallbackWords.Add(w);
                    }
                    words = fallbackWords;
                }

                foreach (var word in words)
                {
                    await _vocabRepo.DeleteAsync(word, cancellationToken);
                    deletedCount++;
                }

                if (words.Count > 0)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _unitOfWork.ClearTracker();
                }

                int pct = (int)((double)deletedCount / total * 90);
                progress?.Report(new ProgressReport(
                    pct,
                    deletedCount,
                    total,
                    $"Đang xóa từ vựng ({deletedCount}/{total})...",
                    $"Deleting words ({deletedCount}/{total})..."));
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            _cacheService?.InvalidateVocabulary();

            progress?.Report(new ProgressReport(100, deletedCount, total, "Xóa dữ liệu hoàn tất!", "Done."));
            return deletedCount;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<TopicDto>> GetAllTopicsAsync(CancellationToken cancellationToken = default)
    {
        var topics = await _vocabRepo.GetAllTopicsAsync(cancellationToken);
        return topics.Select(t => new TopicDto(t.Id, t.Name, t.Description, t.IconGlyph, t.ColorHex)).ToList();
    }

    public async Task<TopicDto> CreateTopicAsync(TopicCreateOrUpdateDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var trimmedName = dto.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new ArgumentException("Topic name cannot be empty.", nameof(dto));
        }

        var existing = await _vocabRepo.GetTopicByNameAsync(trimmedName, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Topic with name '{trimmedName}' already exists.");
        }

        var topic = new Topic
        {
            Name = trimmedName,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            IconGlyph = string.IsNullOrWhiteSpace(dto.IconGlyph) ? "\uE8EC" : dto.IconGlyph.Trim(),
            ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#0078D4" : dto.ColorHex.Trim()
        };

        await _vocabRepo.AddTopicAsync(topic, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TopicDto(topic.Id, topic.Name, topic.Description, topic.IconGlyph, topic.ColorHex);
    }

    public async Task<TopicDto> UpdateTopicAsync(Guid id, TopicCreateOrUpdateDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Cannot update General Context (Guid.Empty).", nameof(id));
        }

        var trimmedName = dto.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new ArgumentException("Topic name cannot be empty.", nameof(dto));
        }

        var topic = await _vocabRepo.GetTopicByIdAsync(id, cancellationToken);
        if (topic == null)
        {
            throw new KeyNotFoundException($"Topic with ID {id} not found.");
        }

        var existingWithName = await _vocabRepo.GetTopicByNameAsync(trimmedName, cancellationToken);
        if (existingWithName != null && existingWithName.Id != id)
        {
            throw new InvalidOperationException($"Topic with name '{trimmedName}' already exists.");
        }

        topic.Name = trimmedName;
        topic.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        if (!string.IsNullOrWhiteSpace(dto.IconGlyph)) topic.IconGlyph = dto.IconGlyph.Trim();
        if (!string.IsNullOrWhiteSpace(dto.ColorHex)) topic.ColorHex = dto.ColorHex.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _cacheService?.InvalidateVocabulary();

        return new TopicDto(topic.Id, topic.Name, topic.Description, topic.IconGlyph, topic.ColorHex);
    }

    public async Task DeleteTopicAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Cannot delete General Context (Guid.Empty).", nameof(id));
        }

        var topic = await _vocabRepo.GetTopicByIdAsync(id, cancellationToken);
        if (topic != null)
        {
            _vocabRepo.DeleteTopic(topic);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _cacheService?.InvalidateVocabulary();
        }
    }

    public async Task AssignWordsToTopicAsync(
        Guid topicId,
        IEnumerable<Guid> wordIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(wordIds);
        if (topicId == Guid.Empty)
        {
            throw new ArgumentException("Cannot assign words directly to General Context (Guid.Empty).", nameof(topicId));
        }

        var topic = await _vocabRepo.GetTopicByIdAsync(topicId, cancellationToken);
        if (topic == null)
        {
            throw new KeyNotFoundException($"Topic with ID {topicId} not found.");
        }

        var idList = wordIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var chunk in idList.Chunk(250))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var existingAssocs = await _vocabRepo.GetExistingWordTopicAssociationsAsync(topicId, chunk, cancellationToken);
                var newAssocs = chunk
                    .Where(wid => !existingAssocs.Contains(wid))
                    .Select(wid => new WordTopic { WordId = wid, TopicId = topicId })
                    .ToList();

                if (newAssocs.Count > 0)
                {
                    _vocabRepo.AddWordTopics(newAssocs);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            _cacheService?.InvalidateVocabulary();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task UnassignWordsFromTopicAsync(
        Guid topicId,
        IEnumerable<Guid> wordIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(wordIds);
        if (topicId == Guid.Empty)
        {
            throw new ArgumentException("Cannot unassign from General Context (Guid.Empty).", nameof(topicId));
        }

        var idList = wordIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var chunk in idList.Chunk(250))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _vocabRepo.RemoveWordTopicAssociationsAsync(topicId, chunk, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            _cacheService?.InvalidateVocabulary();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }


    public async Task<IReadOnlyList<ExamTagDto>> GetAllExamTagsAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _vocabRepo.GetAllExamTagsAsync(cancellationToken);
        return tags.Select(t => new ExamTagDto(t.Id, t.Type, t.Name, t.TargetScore)).ToList();
    }

    public async Task<IReadOnlyList<ContextUnitDto>> GetContextUnitsAsync(CancellationToken cancellationToken = default)
    {
        var topics = await _vocabRepo.GetAllTopicsAsync(cancellationToken);
        var allWords = await _vocabRepo.SearchAsync(take: 1000, cancellationToken: cancellationToken);

        var result = new List<ContextUnitDto>();

        foreach (var topic in topics)
        {
            var unitWords = allWords
                .Where(w => w.WordTopics.Any(wt => wt.TopicId == topic.Id))
                .Select(MapToDto)
                .ToList();

            int remembered = unitWords.Count(w => w.LearningStatus == LearningStatus.Remembered || w.LearningStatus == LearningStatus.Mastered);
            int notRemembered = unitWords.Count(w => w.LearningStatus == LearningStatus.NotRemembered);
            double mastery = unitWords.Count == 0 ? 0.0 : Math.Round((double)remembered / unitWords.Count * 100.0, 1);

            result.Add(new ContextUnitDto(
                topic.Id,
                topic.Name,
                topic.Description,
                topic.IconGlyph ?? "\uE8EC",
                topic.ColorHex ?? "#0078D4",
                unitWords.Count,
                remembered,
                notRemembered,
                mastery,
                unitWords
            ));
        }

        // Untagged words group if any
        var untaggedWords = allWords
            .Where(w => w.WordTopics.Count == 0)
            .Select(MapToDto)
            .ToList();

        if (untaggedWords.Count > 0)
        {
            int remembered = untaggedWords.Count(w => w.LearningStatus == LearningStatus.Remembered || w.LearningStatus == LearningStatus.Mastered);
            int notRemembered = untaggedWords.Count(w => w.LearningStatus == LearningStatus.NotRemembered);
            double mastery = Math.Round((double)remembered / untaggedWords.Count * 100.0, 1);

            result.Add(new ContextUnitDto(
                Guid.Empty,
                "Chủ đề chung (General Context)",
                "Các từ vựng cơ bản và thông dụng hàng ngày",
                "\uE8D2",
                "#69797E",
                untaggedWords.Count,
                remembered,
                notRemembered,
                mastery,
                untaggedWords
            ));
        }

        return result;
    }

    public async Task<WordRelatedSuggestionsDto> GetWordSuggestionsAsync(Guid wordId, CancellationToken cancellationToken = default)
    {
        var word = await GetWordByIdAsync(wordId, cancellationToken);
        if (word == null)
        {
            return new WordRelatedSuggestionsDto(
                wordId,
                string.Empty,
                Array.Empty<WordSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>(),
                Array.Empty<WordSuggestionItemDto>());
        }

        var pool = await SearchWordsAsync(pageSize: 150, cancellationToken: cancellationToken);
        var searchSvc = _smartSearch ?? new SmartSearchService(new FuzzySearchService());
        var suggestionSvc = new VocabularySuggestionService(this, searchSvc);
        return suggestionSvc.GetSuggestions(word, pool);
    }

    public async Task<SmartSearchResultPageDto<WordDto>> SmartSearchWordsAsync(
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
        CancellationToken cancellationToken = default)
    {
        int totalCount = await CountWordsAsync(search, level, pos, topicId, examType, status, isDueToday, isWeak, cancellationToken);
        var words = await SearchWordsAsync(search, level, pos, topicId, examType, status, page, pageSize, sortBy, isAscending, isDueToday, isWeak, cancellationToken);

        var matchResults = new List<SmartSearchMatchResult<WordDto>>();
        if (!string.IsNullOrWhiteSpace(search) && _smartSearch != null)
        {
            foreach (var w in words)
            {
                var scored = _smartSearch.ScoreWord(search, w);
                matchResults.Add(scored ?? new SmartSearchMatchResult<WordDto>(w, 50.0, "Matched"));
            }
        }
        else
        {
            matchResults = words.Select(w => new SmartSearchMatchResult<WordDto>(w, 100.0, "Filter")).ToList();
        }

        IReadOnlyList<string> closestSuggestions = Array.Empty<string>();
        if (totalCount == 0 && !string.IsNullOrWhiteSpace(search))
        {
            closestSuggestions = await GetClosestSuggestionsAsync(search, 5, cancellationToken);
        }

        return new SmartSearchResultPageDto<WordDto>(
            matchResults,
            totalCount,
            page,
            pageSize,
            closestSuggestions);
    }

    public async Task<IReadOnlyList<string>> GetClosestSuggestionsAsync(string query, int maxSuggestions = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2 || _smartSearch == null)
        {
            return Array.Empty<string>();
        }

        var allTerms = await _vocabRepo.GetAllNormalizedTermsAsync(cancellationToken);
        var closest = _smartSearch.FindClosestWordSuggestions(query, allTerms, maxSuggestions);
        if (closest.Count > 0)
        {
            return closest;
        }

        var candidateWords = await _vocabRepo.SearchAsync(take: 200, cancellationToken: cancellationToken);
        var dtos = candidateWords.Select(MapToDto).ToList();
        return _smartSearch.FindClosestWordSuggestions(query, dtos, maxSuggestions);
    }

    private WordDto MapToDto(Word w)
    {
        var meanings = w.Meanings
            .OrderBy(m => m.OrderIndex)
            .Select(m => new WordMeaningDto(
                m.Id,
                m.PartOfSpeech,
                m.DefinitionVi,
                m.DefinitionEn,
                m.Explanation,
                m.OrderIndex,
                m.Examples.Select(e => new ExampleSentenceDto(e.Id, e.SentenceEn, e.SentenceVi, e.AudioPath)).ToList()
            )).ToList();

        var topics = w.WordTopics.Select(wt => wt.Topic?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList();
        var examTags = w.WordExamTags.Select(et => et.ExamTag?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList();

        var rc = w.ReviewCard;
        double retention = rc != null ? _srEngine.CalculateRetentionProbability(rc) : 0.0;

        return new WordDto(
            w.Id,
            w.Term,
            w.PhoneticUk,
            w.PhoneticUs,
            w.AudioUkPath,
            w.AudioUsPath,
            w.Level,
            w.PrimaryPartOfSpeech,
            w.Notes,
            meanings,
            topics,
            examTags,
            rc?.Status,
            rc?.NextReviewAt,
            rc?.Confidence ?? 0.0,
            rc?.IntervalDays ?? 0,
            rc?.EaseFactor ?? 2.5,
            rc?.Lapses ?? 0,
            rc?.Repetitions ?? 0,
            rc?.TotalReviews ?? 0,
            retention
        );
    }
}
