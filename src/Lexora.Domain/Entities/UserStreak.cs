using Lexora.Domain.Common;

namespace Lexora.Domain.Entities;

public class UserStreak : Entity
{
    public int CurrentStreak { get; set; } = 0;
    public int LongestStreak { get; set; } = 0;
    public DateOnly? LastActiveDate { get; set; }
    public int TotalDaysLearned { get; set; } = 0;
}
