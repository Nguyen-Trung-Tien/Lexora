using Lexora.Application.DTOs;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Application.Common;

/// <summary>
/// Provides unified, high-performance mapping extensions from Domain entities to Application DTOs.
/// </summary>
public static class MappingExtensions
{
    /// <summary>
    /// Maps a Word entity with all navigation properties to WordDto.
    /// </summary>
    public static WordDto ToDto(this Word w, ISpacedRepetitionEngine? srEngine = null)
    {
        ArgumentNullException.ThrowIfNull(w);

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

        var topics = w.WordTopics
            .Select(wt => wt.Topic?.Name ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var examTags = w.WordExamTags
            .Select(et => et.ExamTag?.Name ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var rc = w.ReviewCard;
        double retention = rc != null
            ? (srEngine != null ? srEngine.CalculateRetentionProbability(rc) : 100.0)
            : 0.0;

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

    /// <summary>
    /// Maps a ReviewCard entity to ReviewCardDto.
    /// </summary>
    public static ReviewCardDto ToDto(this ReviewCard c, ISpacedRepetitionEngine? srEngine = null)
    {
        ArgumentNullException.ThrowIfNull(c);

        var word = c.Word;
        var primaryMeaning = word?.Meanings?.OrderBy(m => m.OrderIndex).FirstOrDefault();
        var examples = (primaryMeaning?.Examples ?? Enumerable.Empty<ExampleSentence>())
            .Select(e => new ExampleSentenceDto(e.Id, e.SentenceEn, e.SentenceVi, e.AudioPath))
            .ToList();

        double confidence = c.Confidence > 0.0
            ? c.Confidence
            : (c.TotalReviews > 0
                ? Math.Round((double)c.CorrectReviews / c.TotalReviews * 100, 1)
                : (srEngine != null ? srEngine.CalculateRetentionProbability(c) : 50.0));

        return new ReviewCardDto(
            c.Id,
            c.WordId,
            word?.Term ?? string.Empty,
            word?.PhoneticUk,
            word?.PhoneticUs,
            word?.AudioUkPath,
            word?.AudioUsPath,
            word?.PrimaryPartOfSpeech ?? PartOfSpeech.Noun,
            word?.Level ?? CefrLevel.None,
            primaryMeaning?.DefinitionVi ?? string.Empty,
            primaryMeaning?.DefinitionEn,
            examples,
            c.Status,
            c.EaseFactor,
            c.IntervalDays,
            c.Repetitions,
            c.Lapses,
            c.LastReviewedAt,
            c.NextReviewAt,
            c.TotalReviews,
            c.CorrectReviews,
            confidence
        );
    }

    /// <summary>
    /// Maps a GrammarRule entity to GrammarRuleDto.
    /// </summary>
    public static GrammarRuleDto ToDto(this GrammarRule r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var examples = r.Examples
            .Select(e => new GrammarExampleDto(e.Id, e.SentenceEn, e.SentenceVi, e.Note))
            .ToList();

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

    /// <summary>
    /// Maps a HomophoneGroup entity to HomophoneGroupDto.
    /// </summary>
    public static HomophoneGroupDto ToDto(this HomophoneGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        return new HomophoneGroupDto
        {
            Id = group.Id,
            Title = group.Title,
            PhoneticIpa = group.PhoneticIpa,
            Type = group.Type,
            DifficultyLevel = group.DifficultyLevel,
            ExamTag = group.ExamTag,
            DifferentiationTip = group.DifferentiationTip,
            Notes = group.Notes,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
            Items = group.Items.Select(i => new HomophoneItemDto
            {
                Id = i.Id,
                HomophoneGroupId = i.HomophoneGroupId,
                Term = i.Term,
                NormalizedTerm = i.NormalizedTerm,
                PartOfSpeech = i.PartOfSpeech,
                MeaningVi = i.MeaningVi,
                MeaningEn = i.MeaningEn,
                PhoneticSpecific = i.PhoneticSpecific,
                ExampleSentence = i.ExampleSentence,
                ExampleTranslation = i.ExampleTranslation,
                AudioPath = i.AudioPath
            }).ToList()
        };
    }

    /// <summary>
    /// Maps a StudyRoadmap entity to StudyRoadmapDto.
    /// </summary>
    public static StudyRoadmapDto ToDto(this StudyRoadmap entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var milestonesDto = entity.Milestones
            .OrderBy(m => m.OrderIndex)
            .Select(m => new MilestoneDto(
                m.Id,
                m.RoadmapId,
                m.Title,
                m.Description,
                m.OrderIndex,
                m.TargetCompletionDate,
                m.IsCompleted,
                m.Nodes
                    .OrderBy(n => n.OrderIndex)
                    .Select(n => new RoadmapNodeDto(
                        n.Id,
                        n.MilestoneId,
                        n.Title,
                        n.Description,
                        n.Type,
                        n.Status,
                        n.OrderIndex,
                        n.CanvasX,
                        n.CanvasY,
                        n.TargetItemCount,
                        n.CompletedItemCount,
                        n.EaseFactorWeight,
                        n.ReferenceId,
                        n.ReferenceName))
                    .ToList()))
            .ToList();

        return new StudyRoadmapDto(
            entity.Id,
            entity.Title,
            entity.Target,
            entity.StartingScore,
            entity.TargetScore,
            entity.TargetDate,
            entity.IsActive,
            milestonesDto);
    }

    /// <summary>
    /// Maps a LearningStorageItem entity to LearningStorageItemDto.
    /// </summary>
    public static LearningStorageItemDto ToDto(this LearningStorageItem item, ISpacedRepetitionEngine? srEngine = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        var wordDto = item.Word != null ? item.Word.ToDto(srEngine) : null;
        var grammarDto = item.GrammarRule != null ? item.GrammarRule.ToDto() : null;

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
}
