using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities;

public class ReviewCard : Entity
{
    public Guid WordId { get; set; }
    public Word Word { get; set; } = null!;

    public LearningStatus Status { get; set; } = LearningStatus.New;
    public double EaseFactor { get; set; } = 2.5;
    public int IntervalDays { get; set; } = 0;
    public int Repetitions { get; set; } = 0;
    public int Lapses { get; set; } = 0;
    public DateTime? LastReviewedAt { get; set; }
    public DateTime NextReviewAt { get; set; } = DateTime.UtcNow;

    public int TotalReviews { get; set; } = 0;
    public int CorrectReviews { get; set; } = 0;
    public double Confidence => TotalReviews == 0 ? 0.0 : Math.Round((double)CorrectReviews / TotalReviews * 100, 1);

    public ICollection<StudyLog> StudyLogs { get; set; } = new List<StudyLog>();
}
