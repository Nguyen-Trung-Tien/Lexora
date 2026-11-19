using Lexora.Domain.Common;

namespace Lexora.Domain.Entities.Test;

/// <summary>
/// Stores Item Response Theory (1PL/Rasch & Elo-hybrid) calibration parameters
/// for vocabulary words and grammar rules tested in adaptive quizzes.
/// </summary>
public class ItemCalibration : Entity
{
    public Guid? WordId { get; set; }
    public Guid? GrammarRuleId { get; set; }

    /// <summary>
    /// Item difficulty parameter b on logit scale [-3.0 (A1) .. +3.0 (C2)].
    /// </summary>
    public double DifficultyB { get; set; } = 0.0;

    /// <summary>
    /// Item discrimination parameter a (default 1.0).
    /// </summary>
    public double DiscriminationA { get; set; } = 1.0;

    public int AttemptCount { get; set; } = 0;
    public int CorrectCount { get; set; } = 0;
    public int MedianResponseTimeMs { get; set; } = 0;
    public DateTime LastCalibratedAt { get; set; } = DateTime.UtcNow;
}
