using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities;

public class StudyLog : Entity
{
    public Guid? StudySessionId { get; set; }
    public StudySession? StudySession { get; set; }

    public Guid ReviewCardId { get; set; }
    public ReviewCard ReviewCard { get; set; } = null!;

    public ReviewRating Rating { get; set; } = ReviewRating.Good;
    public int ResponseTimeMs { get; set; } = 0;
    public LearningStatus ResultingStatus { get; set; } = LearningStatus.Learning;
    public int ResultingIntervalDays { get; set; } = 0;
}
