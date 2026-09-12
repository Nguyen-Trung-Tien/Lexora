using Lexora.Domain.Common;

namespace Lexora.Domain.Entities;

public class StudySession : Entity
{
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int TotalCards { get; set; } = 0;
    public int CorrectCards { get; set; } = 0;
    public int IncorrectCards { get; set; } = 0;
    public string? SessionType { get; set; } // "DailyReview", "NewWords", "QuickTest"

    public ICollection<StudyLog> StudyLogs { get; set; } = new List<StudyLog>();
}
