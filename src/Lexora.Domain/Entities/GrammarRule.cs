using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities;

public class GrammarRule : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Structure { get; set; } = string.Empty;
    public string ExplanationVi { get; set; } = string.Empty;
    public string? ExplanationEn { get; set; }
    public string? CommonMistakes { get; set; }
    public CefrLevel Level { get; set; } = CefrLevel.None;
    public ExamType ExamTarget { get; set; } = ExamType.General;
    public string? Topic { get; set; }
    public int OrderIndex { get; set; } = 0;

    public ICollection<GrammarExample> Examples { get; set; } = new List<GrammarExample>();
}
