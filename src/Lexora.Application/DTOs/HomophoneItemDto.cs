using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record HomophoneItemDto
{
    public Guid Id { get; init; }
    public Guid HomophoneGroupId { get; init; }
    public string Term { get; init; } = string.Empty;
    public string NormalizedTerm { get; init; } = string.Empty;
    public PartOfSpeech PartOfSpeech { get; init; } = PartOfSpeech.Noun;
    public string MeaningVi { get; init; } = string.Empty;
    public string? MeaningEn { get; init; }
    public string? PhoneticSpecific { get; init; }
    public string? ExampleSentence { get; init; }
    public string? ExampleTranslation { get; init; }
    public string? AudioPath { get; init; }
}
