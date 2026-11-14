using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities;

public class ExamTag : Entity
{
    public ExamType Type { get; set; } = ExamType.General;
    public string Name { get; set; } = string.Empty; // e.g. "TOEIC 650+", "VSTEP B2"
    public string? TargetScore { get; set; }

    public ICollection<WordExamTag> WordExamTags { get; set; } = new List<WordExamTag>();
}
