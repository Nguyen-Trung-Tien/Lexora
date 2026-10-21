using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record CreateHomophoneItemDto
{
    public string Term { get; init; } = string.Empty;
    public PartOfSpeech PartOfSpeech { get; init; } = PartOfSpeech.Noun;
    public string MeaningVi { get; init; } = string.Empty;
    public string? MeaningEn { get; init; }
    public string? PhoneticSpecific { get; init; }
    public string? ExampleSentence { get; init; }
    public string? ExampleTranslation { get; init; }
}

public record CreateHomophoneGroupDto
{
    public string Title { get; init; } = string.Empty;
    public string PhoneticIpa { get; init; } = string.Empty;
    public HomophoneType Type { get; init; } = HomophoneType.ExactHomophone;
    public CefrLevel DifficultyLevel { get; init; } = CefrLevel.None;
    public ExamType ExamTag { get; init; } = ExamType.General;
    public string? DifferentiationTip { get; init; }
    public string? Notes { get; init; }
    public List<CreateHomophoneItemDto> Items { get; init; } = [];
}
