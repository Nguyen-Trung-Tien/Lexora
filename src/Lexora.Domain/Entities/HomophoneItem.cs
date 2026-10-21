using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities;

public class HomophoneItem : Entity
{
    public Guid HomophoneGroupId { get; set; }
    public string Term { get; set; } = string.Empty;
    public string NormalizedTerm { get; set; } = string.Empty;
    public PartOfSpeech PartOfSpeech { get; set; } = PartOfSpeech.Noun;
    public string MeaningVi { get; set; } = string.Empty;
    public string? MeaningEn { get; set; }
    public string? PhoneticSpecific { get; set; }
    public string? ExampleSentence { get; set; }
    public string? ExampleTranslation { get; set; }
    public string? AudioPath { get; set; }

    // Navigation property
    public HomophoneGroup? Group { get; set; }
}
