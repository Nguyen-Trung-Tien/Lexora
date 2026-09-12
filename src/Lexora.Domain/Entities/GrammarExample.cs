using Lexora.Domain.Common;

namespace Lexora.Domain.Entities;

public class GrammarExample : Entity
{
    public Guid GrammarRuleId { get; set; }
    public GrammarRule GrammarRule { get; set; } = null!;

    public string SentenceEn { get; set; } = string.Empty;
    public string SentenceVi { get; set; } = string.Empty;
    public string? Note { get; set; }
}
