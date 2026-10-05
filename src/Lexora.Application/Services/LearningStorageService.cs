using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class LearningStorageService : ILearningStorageService
{
    public static event EventHandler? GlobalStorageChanged;
    public event EventHandler? StorageChanged
    {
        add => GlobalStorageChanged += value;
        remove => GlobalStorageChanged -= value;
    }

    private void NotifyStorageChanged()
    {
        GlobalStorageChanged?.Invoke(this, EventArgs.Empty);
    }

    private readonly ILearningStorageRepository _storageRepo;
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IGrammarRepository _grammarRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly ISpacedRepetitionEngine _srEngine;
    private readonly ITestService _testService;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string[] FallbackVocabDistractors =
    {
        "Hành động hoặc hoạt động cụ thể",
        "Trạng thái hoặc điều kiện tồn tại",
        "Tính chất hoặc đặc điểm nổi bật",
        "Đối tượng, sự vật hoặc hiện tượng",
        "Cách thức hoặc phương thức thực hiện"
    };

    private static readonly string[] FallbackGrammarDistractors =
    {
        "S + V + O",
        "S + to be + Adj / Noun",
        "S + modal verb + V-inf",
        "S + have/has + V3/ed",
        "S + Aux + V-ing",
        "It + is + Adj + to V-inf"
    };

    public LearningStorageService(
        ILearningStorageRepository storageRepo,
        IVocabularyRepository vocabRepo,
        IGrammarRepository grammarRepo,
        IReviewRepository reviewRepo,
        ISpacedRepetitionEngine srEngine,
        ITestService testService,
        IUnitOfWork unitOfWork)
    {
        _storageRepo = storageRepo;
        _vocabRepo = vocabRepo;
        _grammarRepo = grammarRepo;
        _reviewRepo = reviewRepo;
        _srEngine = srEngine;
        _testService = testService;
        _unitOfWork = unitOfWork;
    }

    public async Task<StoragePagedResultDto> GetPagedItemsAsync(
        StorageItemType? itemType = null,
        StorageLearningStatus? status = null,
        string? search = null,
        bool? dueTodayOnly = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        int validPage = Math.Max(1, page);
        int validPageSize = Math.Clamp(pageSize, 5, 100);
        int skip = (validPage - 1) * validPageSize;

        var items = await _storageRepo.GetFilteredPagedAsync(
            itemType, status, search, dueTodayOnly, skip, validPageSize, cancellationToken);
        var totalCount = await _storageRepo.CountFilteredAsync(
            itemType, status, search, dueTodayOnly, cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new StoragePagedResultDto(dtos, totalCount, validPage, validPageSize);
    }

    public async Task<StorageStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var stats = await _storageRepo.GetStatsAsync(cancellationToken);
        return new StorageStatisticsDto(
            stats.Total,
            stats.VocabCount,
            stats.GrammarCount,
            stats.NewCount,
            stats.LearningCount,
            stats.WeakCount,
            stats.MasteredCount,
            stats.DueTodayCount,
            stats.AvgRetention
        );
    }

    public async Task<LearningStorageItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _storageRepo.GetWithDetailsAsync(id, cancellationToken);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<LearningStorageItemDto?> GetByWordIdAsync(Guid wordId, CancellationToken cancellationToken = default)
    {
        var item = await _storageRepo.GetByWordIdAsync(wordId, cancellationToken);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<LearningStorageItemDto?> GetByGrammarRuleIdAsync(Guid grammarRuleId, CancellationToken cancellationToken = default)
    {
        var item = await _storageRepo.GetByGrammarRuleIdAsync(grammarRuleId, cancellationToken);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<LearningStorageItemDto> MoveWordToStorageAsync(
        Guid wordId,
        StorageAdditionReason reason = StorageAdditionReason.Manual,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _storageRepo.GetByWordIdAsync(wordId, cancellationToken);
        if (existing != null)
        {
            if (reason != StorageAdditionReason.Manual)
            {
                existing.ReasonAdded = reason;
            }
            if (!string.IsNullOrWhiteSpace(note))
            {
                existing.Note = note.Trim();
            }
            if (existing.Status == StorageLearningStatus.Mastered && reason != StorageAdditionReason.Manual)
            {
                existing.Status = StorageLearningStatus.Weak;
                existing.NextReviewAt = DateTime.UtcNow;
                existing.IntervalDays = 1;
            }
            existing.UpdatedAt = DateTime.UtcNow;
            await _storageRepo.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            NotifyStorageChanged();
            return MapToDto(existing);
        }

        var word = await _vocabRepo.GetWithDetailsAsync(wordId, cancellationToken)
                   ?? throw new InvalidOperationException($"Word with ID {wordId} was not found.");

        var card = word.ReviewCard;
        var initialStatus = (reason == StorageAdditionReason.MultipleErrors || reason == StorageAdditionReason.Lapsed)
            ? StorageLearningStatus.Weak
            : StorageLearningStatus.Learning;
        double initialRetention = 50.0;

        if (card != null)
        {
            if (card.Lapses > 0 || card.Status == LearningStatus.NotRemembered || reason == StorageAdditionReason.MultipleErrors)
            {
                initialStatus = StorageLearningStatus.Weak;
            }
            else if (card.Status == LearningStatus.Mastered && reason == StorageAdditionReason.Manual)
            {
                initialStatus = StorageLearningStatus.Mastered;
            }
            initialRetention = _srEngine.CalculateRetentionProbability(card);
        }

        var newItem = new LearningStorageItem
        {
            ItemType = StorageItemType.Vocabulary,
            WordId = word.Id,
            Status = initialStatus,
            ReasonAdded = reason,
            Note = note?.Trim(),
            EaseFactor = card?.EaseFactor ?? 2.5,
            IntervalDays = card?.IntervalDays > 0 ? card.IntervalDays : 1,
            Repetitions = card?.Repetitions ?? 0,
            Lapses = card?.Lapses ?? 0,
            RetentionProbability = initialRetention,
            NextReviewAt = card?.NextReviewAt ?? DateTime.UtcNow,
            LastReviewedAt = card?.LastReviewedAt
        };

        await _storageRepo.AddAsync(newItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        NotifyStorageChanged();

        var saved = await _storageRepo.GetWithDetailsAsync(newItem.Id, cancellationToken);
        return MapToDto(saved ?? newItem);
    }

    public async Task<LearningStorageItemDto> MoveGrammarToStorageAsync(
        Guid grammarRuleId,
        StorageAdditionReason reason = StorageAdditionReason.Manual,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _storageRepo.GetByGrammarRuleIdAsync(grammarRuleId, cancellationToken);
        if (existing != null)
        {
            if (reason != StorageAdditionReason.Manual)
            {
                existing.ReasonAdded = reason;
            }
            if (!string.IsNullOrWhiteSpace(note))
            {
                existing.Note = note.Trim();
            }
            if (existing.Status == StorageLearningStatus.Mastered && reason != StorageAdditionReason.Manual)
            {
                existing.Status = StorageLearningStatus.Weak;
                existing.NextReviewAt = DateTime.UtcNow;
                existing.IntervalDays = 1;
            }
            existing.UpdatedAt = DateTime.UtcNow;
            await _storageRepo.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            NotifyStorageChanged();
            return MapToDto(existing);
        }

        var rule = await _grammarRepo.GetWithExamplesAsync(grammarRuleId, cancellationToken)
                   ?? throw new InvalidOperationException($"Grammar rule with ID {grammarRuleId} was not found.");

        var newItem = new LearningStorageItem
        {
            ItemType = StorageItemType.Grammar,
            GrammarRuleId = rule.Id,
            Status = reason == StorageAdditionReason.MultipleErrors ? StorageLearningStatus.Weak : StorageLearningStatus.Learning,
            ReasonAdded = reason,
            Note = note?.Trim(),
            EaseFactor = 2.5,
            IntervalDays = 1,
            Repetitions = 0,
            Lapses = 0,
            RetentionProbability = 50.0,
            NextReviewAt = DateTime.UtcNow
        };

        await _storageRepo.AddAsync(newItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        NotifyStorageChanged();

        var saved = await _storageRepo.GetWithDetailsAsync(newItem.Id, cancellationToken);
        return MapToDto(saved ?? newItem);
    }

    public async Task<int> MoveWordsToStorageAsync(
        IEnumerable<Guid> wordIds,
        StorageAdditionReason reason = StorageAdditionReason.Manual,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(wordIds);
        var idList = wordIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return 0;

        int added = 0;
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var id in idList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var existing = await _storageRepo.GetByWordIdAsync(id, cancellationToken);
                if (existing != null)
                {
                    if (reason != StorageAdditionReason.Manual)
                    {
                        existing.ReasonAdded = reason;
                    }
                    if (!string.IsNullOrWhiteSpace(note))
                    {
                        existing.Note = note.Trim();
                    }
                    if (existing.Status == StorageLearningStatus.Mastered && reason != StorageAdditionReason.Manual)
                    {
                        existing.Status = StorageLearningStatus.Weak;
                        existing.NextReviewAt = DateTime.UtcNow;
                        existing.IntervalDays = 1;
                    }
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _storageRepo.UpdateAsync(existing, cancellationToken);
                }
                else
                {
                    var word = await _vocabRepo.GetWithDetailsAsync(id, cancellationToken);
                    if (word == null) continue;

                    var card = await _reviewRepo.GetByWordIdAsync(id, cancellationToken);

                    var newItem = new LearningStorageItem
                    {
                        ItemType = StorageItemType.Vocabulary,
                        WordId = word.Id,
                        Status = reason == StorageAdditionReason.MultipleErrors ? StorageLearningStatus.Weak : StorageLearningStatus.Learning,
                        ReasonAdded = reason,
                        Note = note?.Trim(),
                        EaseFactor = card?.EaseFactor ?? 2.5,
                        IntervalDays = card?.IntervalDays ?? 1,
                        Repetitions = card?.Repetitions ?? 0,
                        Lapses = card?.Lapses ?? 0,
                        RetentionProbability = 50.0,
                        NextReviewAt = DateTime.UtcNow,
                        LastReviewedAt = card?.LastReviewedAt
                    };

                    await _storageRepo.AddAsync(newItem, cancellationToken);
                    added++;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        NotifyStorageChanged();
        return added;
    }

    public async Task<int> MoveGrammarsToStorageAsync(
        IEnumerable<Guid> grammarRuleIds,
        StorageAdditionReason reason = StorageAdditionReason.Manual,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grammarRuleIds);
        var idList = grammarRuleIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return 0;

        int added = 0;
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var id in idList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var existing = await _storageRepo.GetByGrammarRuleIdAsync(id, cancellationToken);
                if (existing != null)
                {
                    if (reason != StorageAdditionReason.Manual)
                    {
                        existing.ReasonAdded = reason;
                    }
                    if (!string.IsNullOrWhiteSpace(note))
                    {
                        existing.Note = note.Trim();
                    }
                    if (existing.Status == StorageLearningStatus.Mastered && reason != StorageAdditionReason.Manual)
                    {
                        existing.Status = StorageLearningStatus.Weak;
                        existing.NextReviewAt = DateTime.UtcNow;
                        existing.IntervalDays = 1;
                    }
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _storageRepo.UpdateAsync(existing, cancellationToken);
                }
                else
                {
                    var rule = await _grammarRepo.GetWithExamplesAsync(id, cancellationToken);
                    if (rule == null) continue;

                    var newItem = new LearningStorageItem
                    {
                        ItemType = StorageItemType.Grammar,
                        GrammarRuleId = rule.Id,
                        Status = reason == StorageAdditionReason.MultipleErrors ? StorageLearningStatus.Weak : StorageLearningStatus.Learning,
                        ReasonAdded = reason,
                        Note = note?.Trim(),
                        EaseFactor = 2.5,
                        IntervalDays = 1,
                        Repetitions = 0,
                        Lapses = 0,
                        RetentionProbability = 50.0,
                        NextReviewAt = DateTime.UtcNow
                    };

                    await _storageRepo.AddAsync(newItem, cancellationToken);
                    added++;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        NotifyStorageChanged();
        return added;
    }

    public async Task RemoveFromStorageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _storageRepo.GetByIdAsync(id, cancellationToken);
        if (item != null)
        {
            await _storageRepo.DeleteAsync(item, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            NotifyStorageChanged();
        }
    }

    public async Task<int> RemoveRangeFromStorageAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var idList = ids.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0) return 0;

        progress?.Report(new ProgressReport(0, 0, idList.Count, "Bắt đầu xóa khỏi kho lưu trữ...", "Preparing deletion..."));

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            int deletedCount = 0;
            int total = idList.Count;
            var chunks = idList.Chunk(150).ToList();

            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var count = await _storageRepo.DeleteRangeAsync(chunk, cancellationToken);
                deletedCount += count;

                if (count > 0)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _unitOfWork.ClearTracker();
                }

                int pct = (int)((double)deletedCount / total * 90);
                progress?.Report(new ProgressReport(
                    pct,
                    deletedCount,
                    total,
                    $"Đang xóa khỏi kho lưu trữ ({deletedCount}/{total})...",
                    $"Removing from storage ({deletedCount}/{total})..."));
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            NotifyStorageChanged();

            progress?.Report(new ProgressReport(100, deletedCount, total, "Xóa khỏi kho lưu trữ hoàn tất!", "Done."));
            return deletedCount;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task MarkAsMasteredAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _storageRepo.GetByIdAsync(id, cancellationToken);
        if (item == null) return;

        item.Status = StorageLearningStatus.Mastered;
        item.Repetitions = Math.Max(item.Repetitions, 5);
        item.IntervalDays = Math.Max(item.IntervalDays * 2, 14);
        item.NextReviewAt = DateTime.UtcNow.AddDays(item.IntervalDays);
        item.RetentionProbability = 95.0;
        item.LastReviewedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        // If it's a word, sync review card if present
        if (item.WordId.HasValue)
        {
            var card = await _reviewRepo.GetByWordIdAsync(item.WordId.Value, cancellationToken);
            if (card != null)
            {
                card.Status = LearningStatus.Mastered;
                card.IntervalDays = item.IntervalDays;
                card.NextReviewAt = item.NextReviewAt;
                card.UpdatedAt = DateTime.UtcNow;
                await _reviewRepo.UpdateAsync(card, cancellationToken);
            }
        }

        await _storageRepo.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        NotifyStorageChanged();
    }

    public async Task<int> MarkRangeAsMasteredAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        int count = 0;
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = await _storageRepo.GetByIdAsync(id, cancellationToken);
            if (item != null)
            {
                item.Status = StorageLearningStatus.Mastered;
                item.IntervalDays = Math.Max(item.IntervalDays * 2, 14);
                item.NextReviewAt = DateTime.UtcNow.AddDays(item.IntervalDays);
                item.RetentionProbability = 95.0;
                item.UpdatedAt = DateTime.UtcNow;
                await _storageRepo.UpdateAsync(item, cancellationToken);
                count++;
            }
        }
        if (count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            NotifyStorageChanged();
        }
        return count;
    }

    public async Task UpdateStatusAsync(Guid id, StorageLearningStatus newStatus, CancellationToken cancellationToken = default)
    {
        var item = await _storageRepo.GetByIdAsync(id, cancellationToken);
        if (item == null) return;

        item.Status = newStatus;
        if (newStatus == StorageLearningStatus.Weak)
        {
            item.IntervalDays = 1;
            item.NextReviewAt = DateTime.UtcNow;
            item.RetentionProbability = Math.Min(item.RetentionProbability, 40.0);
        }
        else if (newStatus == StorageLearningStatus.Mastered)
        {
            item.IntervalDays = Math.Max(item.IntervalDays, 14);
            item.NextReviewAt = DateTime.UtcNow.AddDays(item.IntervalDays);
            item.RetentionProbability = 95.0;
        }

        item.UpdatedAt = DateTime.UtcNow;
        await _storageRepo.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        NotifyStorageChanged();
    }

    public async Task UpdateNoteAsync(Guid id, string? note, CancellationToken cancellationToken = default)
    {
        var item = await _storageRepo.GetByIdAsync(id, cancellationToken);
        if (item == null) return;

        item.Note = note?.Trim();
        item.UpdatedAt = DateTime.UtcNow;
        await _storageRepo.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        NotifyStorageChanged();
    }

    public async Task<IReadOnlySet<Guid>> GetStoredWordIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _storageRepo.GetStoredWordIdsAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetStoredGrammarRuleIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _storageRepo.GetStoredGrammarRuleIdsAsync(cancellationToken);
    }

    public async Task AutoCheckAndAddToStorageAsync(
        Guid wordId,
        int consecutiveErrors,
        double retentionRate,
        CancellationToken cancellationToken = default)
    {
        if (consecutiveErrors >= 2 || retentionRate < 60.0)
        {
            var reason = consecutiveErrors >= 2
                ? StorageAdditionReason.MultipleErrors
                : StorageAdditionReason.LowRetention;

            await MoveWordToStorageAsync(wordId, reason, null, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<StoragePracticeQuestionDto>> GeneratePracticeAsync(
        StorageItemType itemType,
        int limit = 15,
        bool prioritizeWeak = true,
        CancellationToken cancellationToken = default)
    {
        var items = await _storageRepo.GetItemsForPracticeAsync(itemType, limit, prioritizeWeak, cancellationToken);
        if (items.Count == 0) return Array.Empty<StoragePracticeQuestionDto>();

        var questions = new List<StoragePracticeQuestionDto>();

        if (itemType == StorageItemType.Vocabulary)
        {
            // Load distractor definitions
            var allWords = await _vocabRepo.SearchAsync(take: 100, cancellationToken: cancellationToken) ?? Array.Empty<Word>();
            var distractorPool = allWords
                .SelectMany(w => w?.Meanings ?? Enumerable.Empty<WordMeaning>())
                .Select(m => m?.DefinitionVi)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d!)
                .Distinct()
                .ToList();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var word = item.Word;
                if (word == null) continue;

                var meaning = word.Meanings.FirstOrDefault();
                string correctAnswer = meaning?.DefinitionVi ?? "Định nghĩa";
                string prompt = $"What is the Vietnamese meaning of \"{word.Term}\"?";
                string subPrompt = $"{word.PhoneticUs ?? word.PhoneticUk} • {word.PrimaryPartOfSpeech}";

                var options = new List<string> { correctAnswer };
                var distractors = distractorPool
                    .Where(d => !d.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();
                options.AddRange(distractors);

                if (options.Count < 4)
                {
                    foreach (var fallback in FallbackVocabDistractors)
                    {
                        if (options.Count >= 4) break;
                        if (!options.Any(o => o.Equals(fallback, StringComparison.OrdinalIgnoreCase)))
                        {
                            options.Add(fallback);
                        }
                    }
                }

                var shuffled = options.Distinct().OrderBy(_ => Random.Shared.Next()).ToList();
                var explanation = $"{word.Term}: {correctAnswer}";
                if (meaning?.Examples.FirstOrDefault() is { } ex)
                {
                    explanation += $"\nVí dụ: {ex.SentenceEn} ({ex.SentenceVi})";
                }

                questions.Add(new StoragePracticeQuestionDto(
                    item.Id,
                    StorageItemType.Vocabulary,
                    word.Id,
                    i + 1,
                    prompt,
                    subPrompt,
                    word.AudioUsPath ?? word.AudioUkPath,
                    correctAnswer,
                    shuffled,
                    explanation,
                    Word: MapWordToDto(word)
                ));
            }
        }
        else
        {
            // Grammar Practice
            var allRules = await _grammarRepo.GetAllAsync(cancellationToken);
            var allStructures = allRules.Select(r => r.Structure).Distinct().ToList();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var rule = item.GrammarRule;
                if (rule == null) continue;

                var example = rule.Examples.FirstOrDefault();
                string prompt;
                string? subPrompt;
                string correctAnswer = rule.Structure;

                if (example != null && !string.IsNullOrWhiteSpace(example.SentenceEn))
                {
                    prompt = $"Which grammar formula applies to:\n\"{example.SentenceEn}\"?";
                    subPrompt = $"Ý nghĩa: {example.SentenceVi}";
                }
                else
                {
                    prompt = $"What is the correct structure for \"{rule.Title}\"?";
                    subPrompt = $"Target level: {rule.Level} • Exam: {rule.ExamTarget}";
                }

                var options = new List<string> { correctAnswer };
                var distractors = allStructures
                    .Where(s => !s.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();
                options.AddRange(distractors);

                if (options.Count < 4)
                {
                    foreach (var fallback in FallbackGrammarDistractors)
                    {
                        if (options.Count >= 4) break;
                        if (!options.Any(o => o.Equals(fallback, StringComparison.OrdinalIgnoreCase)))
                        {
                            options.Add(fallback);
                        }
                    }
                }

                var shuffled = options.Distinct().OrderBy(_ => Random.Shared.Next()).ToList();
                var explanation = $"{rule.Title}: {rule.Structure}\nGiải thích: {rule.ExplanationVi}";

                questions.Add(new StoragePracticeQuestionDto(
                    item.Id,
                    StorageItemType.Grammar,
                    rule.Id,
                    i + 1,
                    prompt,
                    subPrompt,
                    null,
                    correctAnswer,
                    shuffled,
                    explanation,
                    GrammarRule: MapGrammarToDto(rule)
                ));
            }
        }

        return questions;
    }

    public async Task<StoragePracticeResultDto> EvaluatePracticeAsync(
        IReadOnlyList<StoragePracticeQuestionDto> questions,
        IReadOnlyList<StoragePracticeSubmissionDto> answers,
        CancellationToken cancellationToken = default)
    {
        int correct = 0;
        int incorrect = 0;
        var evaluatedQuestions = new List<StoragePracticeQuestionDto>();

        foreach (var q in questions)
        {
            var submission = answers.FirstOrDefault(a => a.StorageItemId == q.StorageItemId);
            string userAns = submission?.SelectedAnswer?.Trim() ?? string.Empty;
            bool isCorrect = string.Equals(userAns, q.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

            if (isCorrect) correct++;
            else incorrect++;

            evaluatedQuestions.Add(q with
            {
                UserAnswer = userAns,
                IsCorrect = isCorrect
            });

            // Update Storage Item learning progress
            var item = await _storageRepo.GetByIdAsync(q.StorageItemId, cancellationToken);
            if (item != null)
            {
                item.LastReviewedAt = DateTime.UtcNow;
                item.UpdatedAt = DateTime.UtcNow;

                if (isCorrect)
                {
                    item.CorrectCount++;
                    item.Repetitions++;
                    // Status transition
                    if (item.Status == StorageLearningStatus.Weak && item.CorrectCount >= 2)
                    {
                        item.Status = StorageLearningStatus.Learning;
                    }
                    else if (item.Status == StorageLearningStatus.Learning && item.Repetitions >= 4)
                    {
                        item.Status = StorageLearningStatus.Mastered;
                    }

                    // SM-2 interval progression
                    item.IntervalDays = item.Repetitions switch
                    {
                        1 => 1,
                        2 => 3,
                        3 => 6,
                        _ => Math.Max(1, (int)Math.Round(item.IntervalDays * item.EaseFactor))
                    };
                    item.NextReviewAt = DateTime.UtcNow.AddDays(item.IntervalDays);
                    item.RetentionProbability = Math.Min(100.0, item.RetentionProbability + 15.0);
                }
                else
                {
                    item.IncorrectCount++;
                    item.Lapses++;
                    item.Repetitions = 0;
                    item.Status = StorageLearningStatus.Weak; // demote to Weak immediately
                    item.EaseFactor = Math.Max(1.3, Math.Round(item.EaseFactor - 0.20, 2)); // Ease factor floor 1.3 invariant
                    item.IntervalDays = 1;
                    item.NextReviewAt = DateTime.UtcNow; // Review due immediately
                    item.RetentionProbability = Math.Max(10.0, item.RetentionProbability - 25.0);
                }

                await _storageRepo.UpdateAsync(item, cancellationToken);

                // If Vocabulary, sync with Word ReviewCard as well
                if (item.WordId.HasValue)
                {
                    var card = await _reviewRepo.GetByWordIdAsync(item.WordId.Value, cancellationToken);
                    if (card != null)
                    {
                        card.TotalReviews++;
                        if (isCorrect)
                        {
                            card.CorrectReviews++;
                            card.Repetitions++;
                            if (card.Repetitions >= 5) card.Status = LearningStatus.Mastered;
                            else card.Status = LearningStatus.Remembered;
                            card.IntervalDays = item.IntervalDays;
                            card.NextReviewAt = item.NextReviewAt;
                        }
                        else
                        {
                            card.Lapses++;
                            card.Repetitions = 0;
                            card.Status = LearningStatus.NotRemembered;
                            card.EaseFactor = Math.Max(1.3, card.EaseFactor - 0.20);
                            card.IntervalDays = 1;
                            card.NextReviewAt = DateTime.UtcNow;
                        }
                        card.LastReviewedAt = DateTime.UtcNow;
                        card.UpdatedAt = DateTime.UtcNow;
                        await _reviewRepo.UpdateAsync(card, cancellationToken);

                        var studyLog = new StudyLog
                        {
                            ReviewCardId = card.Id,
                            Rating = isCorrect ? ReviewRating.Good : ReviewRating.Again,
                            ResponseTimeMs = submission?.ResponseTimeMs ?? 0,
                            ResultingStatus = card.Status,
                            ResultingIntervalDays = card.IntervalDays,
                            CreatedAt = DateTime.UtcNow
                        };
                        _reviewRepo.AddStudyLog(studyLog);
                    }
                }
            }
        }

        await _reviewRepo.UpdateStreakAsync(DateOnly.FromDateTime(DateTime.Now), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        int total = evaluatedQuestions.Count;
        double scorePercent = total > 0 ? Math.Round((double)correct / total * 100.0, 1) : 0.0;

        return new StoragePracticeResultDto(
            total,
            correct,
            incorrect,
            scorePercent,
            evaluatedQuestions
        );
    }

    public async Task<TestSessionDto> GenerateStorageTestAsync(
        int questionCount = 10,
        StorageItemType? itemType = null,
        bool prioritizeWeak = true,
        CancellationToken cancellationToken = default)
    {
        var testItems = await _storageRepo.GetItemsForTestAsync(itemType, questionCount, prioritizeWeak, cancellationToken);
        if (testItems.Count == 0)
        {
            throw new InvalidOperationException("No items available in Learning Storage to generate a test.");
        }

        var wordIds = testItems.Where(x => x.WordId.HasValue).Select(x => x.WordId!.Value).ToList();
        if (wordIds.Count > 0)
        {
            var config = new TestConfigurationDto(
                QuestionCount: Math.Min(questionCount, wordIds.Count),
                CustomWordIds: wordIds,
                WeakWordsOnly: false
            );
            return await _testService.GenerateTestAsync(config, cancellationToken);
        }

        throw new InvalidOperationException("No suitable words found in Storage for test generation.");
    }

    private LearningStorageItemDto MapToDto(LearningStorageItem item)
    {
        var wordDto = item.Word != null ? MapWordToDto(item.Word) : null;
        var grammarDto = item.GrammarRule != null ? MapGrammarToDto(item.GrammarRule) : null;

        return new LearningStorageItemDto(
            item.Id,
            item.ItemType,
            item.WordId,
            wordDto,
            item.GrammarRuleId,
            grammarDto,
            item.Status,
            item.ReasonAdded,
            item.Note,
            item.IncorrectCount,
            item.CorrectCount,
            item.EaseFactor,
            item.IntervalDays,
            item.Repetitions,
            item.Lapses,
            item.RetentionProbability,
            item.LastReviewedAt,
            item.NextReviewAt,
            item.CreatedAt
        );
    }

    private WordDto MapWordToDto(Word w)
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
            rc != null ? _srEngine.CalculateRetentionProbability(rc) : 0.0
        );
    }

    private static GrammarRuleDto MapGrammarToDto(GrammarRule r)
    {
        var examples = r.Examples.Select(e => new GrammarExampleDto(e.Id, e.SentenceEn, e.SentenceVi, e.Note)).ToList();
        return new GrammarRuleDto(
            r.Id,
            r.Title,
            r.Structure,
            r.ExplanationVi,
            r.ExplanationEn,
            r.CommonMistakes,
            r.Level,
            r.ExamTarget,
            r.Topic,
            examples
        );
    }
}
