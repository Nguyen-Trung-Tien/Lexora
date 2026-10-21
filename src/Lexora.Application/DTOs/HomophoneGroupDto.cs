using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record HomophoneGroupDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string PhoneticIpa { get; init; } = string.Empty;
    public HomophoneType Type { get; init; } = HomophoneType.ExactHomophone;
    public CefrLevel DifficultyLevel { get; init; } = CefrLevel.None;
    public ExamType ExamTag { get; init; } = ExamType.General;
    public string? DifferentiationTip { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public IReadOnlyList<HomophoneItemDto> Items { get; init; } = [];

    // Calculated helper properties for UI bindings
    public int ItemCount => Items.Count;
    public string TermsSummary => string.Join(" / ", Items.Select(i => i.Term));
}
