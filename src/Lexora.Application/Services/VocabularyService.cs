using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class VocabularyService : IVocabularyService
{
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly IUnitOfWork _unitOfWork;

    public VocabularyService(
        IVocabularyRepository vocabRepo,
        IReviewRepository reviewRepo,
        IUnitOfWork unitOfWork)
    {
        _vocabRepo = vocabRepo;
        _reviewRepo = reviewRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<WordDto?> GetWordByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var word = await _vocabRepo.GetWithDetailsAsync(id, cancellationToken);
        return word == null ? null : MapToDto(word);
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
        CancellationToken cancellationToken = default)
    {
        int skip = Math.Max(0, (page - 1) * pageSize);
        var words = await _vocabRepo.SearchAsync(
            search, level, pos, topicId, examType, status,
            skip, pageSize, sortBy, isAscending, cancellationToken);

        return words.Select(MapToDto).ToList();
    }

    public async Task<int> CountWordsAsync(
        string? search = null,
        CefrLevel? level = null,
        PartOfSpeech? pos = null,
        Guid? topicId = null,
        ExamType? examType = null,
        LearningStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        return await _vocabRepo.CountFilteredAsync(search, level, pos, topicId, examType, status, cancellationToken);
    }

    public async Task<WordDto> CreateWordAsync(WordCreateOrUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var word = new Word
        {
            Term = dto.Term.Trim(),
            NormalizedTerm = dto.Term.Trim().ToLowerInvariant(),
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

        // Fetch back with full details
        var savedWord = await _vocabRepo.GetWithDetailsAsync(word.Id, cancellationToken);
        return MapToDto(savedWord ?? word);
    }

    public async Task<WordDto> UpdateWordAsync(WordCreateOrUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Id == null)
            throw new ArgumentException("Word Id must not be null for update", nameof(dto));

        var word = await _vocabRepo.GetWithDetailsAsync(dto.Id.Value, cancellationToken);
        if (word == null)
            throw new KeyNotFoundException($"Word with ID {dto.Id} not found");

        word.Term = dto.Term.Trim();
        word.NormalizedTerm = dto.Term.Trim().ToLowerInvariant();
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        _vocabRepo.AddMeanings(newMeanings);

        // Update Topics
        var newTopics = dto.TopicIds.Select(tid => new WordTopic { WordId = word.Id, TopicId = tid }).ToList();
        _vocabRepo.AddWordTopics(newTopics);

        // Update Exam Tags
        var newTags = dto.ExamTagIds.Select(etid => new WordExamTag { WordId = word.Id, ExamTagId = etid }).ToList();
        _vocabRepo.AddWordExamTags(newTags);

        await _vocabRepo.UpdateAsync(word, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var savedWord = await _vocabRepo.GetWithDetailsAsync(word.Id, cancellationToken);
        return MapToDto(savedWord ?? word);
    }

    public async Task DeleteWordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var word = await _vocabRepo.GetByIdAsync(id, cancellationToken);
        if (word != null)
        {
            await _vocabRepo.DeleteAsync(word, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<TopicDto>> GetAllTopicsAsync(CancellationToken cancellationToken = default)
    {
        var topics = await _vocabRepo.GetAllTopicsAsync(cancellationToken);
        return topics.Select(t => new TopicDto(t.Id, t.Name, t.Description, t.IconGlyph, t.ColorHex)).ToList();
    }

    public async Task<IReadOnlyList<ExamTagDto>> GetAllExamTagsAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _vocabRepo.GetAllExamTagsAsync(cancellationToken);
        return tags.Select(t => new ExamTagDto(t.Id, t.Type, t.Name, t.TargetScore)).ToList();
    }

    private static WordDto MapToDto(Word w)
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
            w.ReviewCard?.Status,
            w.ReviewCard?.NextReviewAt,
            w.ReviewCard?.Confidence ?? 0.0
        );
    }
}
