using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities.MemoryTips;

/// <summary>
/// Domain POCO entity representing a high-yield memory hook, mnemonic rule,
/// or exam strategy tip for English learning.
/// </summary>
public class MemoryTip : Entity
{
    public string Title { get; set; } = string.Empty;
    public string MnemonicHook { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string? Example { get; set; }
    public string? CommonTrap { get; set; }
    public TipCategory Category { get; set; } = TipCategory.Vocabulary;
    public MnemonicTechnique Technique { get; set; } = MnemonicTechnique.Acrostic;
    public ExamTarget? ExamTarget { get; set; }
    public bool IsPinned { get; set; }
    public string? Tags { get; set; }

    // Optional foreign keys to connect with other core domain entities
    public Guid? LinkedWordId { get; set; }
    public Guid? LinkedGrammarRuleId { get; set; }
}
