using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities;

public class HomophoneGroup : Entity
{
    public string Title { get; set; } = string.Empty;
    public string PhoneticIpa { get; set; } = string.Empty;
    public HomophoneType Type { get; set; } = HomophoneType.ExactHomophone;
    public CefrLevel DifficultyLevel { get; set; } = CefrLevel.None;
    public ExamType ExamTag { get; set; } = ExamType.General;
    public string? DifferentiationTip { get; set; }
    public string? Notes { get; set; }

    // Navigation property
    public ICollection<HomophoneItem> Items { get; set; } = new List<HomophoneItem>();
}
