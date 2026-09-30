using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities;

public class LearningStorageItem : Entity
{
    public StorageItemType ItemType { get; set; }

    public Guid? WordId { get; set; }
    public Word? Word { get; set; }

    public Guid? GrammarRuleId { get; set; }
    public GrammarRule? GrammarRule { get; set; }

    public StorageLearningStatus Status { get; set; } = StorageLearningStatus.Learning;
    public StorageAdditionReason ReasonAdded { get; set; } = StorageAdditionReason.Manual;

    public string? Note { get; set; }

    public int IncorrectCount { get; set; } = 0;
    public int CorrectCount { get; set; } = 0;

    public double EaseFactor { get; set; } = 2.5;
    public int IntervalDays { get; set; } = 1;
    public int Repetitions { get; set; } = 0;
    public int Lapses { get; set; } = 0;

    public double RetentionProbability { get; set; } = 50.0;
    public DateTime? LastReviewedAt { get; set; }
    public DateTime NextReviewAt { get; set; } = DateTime.UtcNow;
}
