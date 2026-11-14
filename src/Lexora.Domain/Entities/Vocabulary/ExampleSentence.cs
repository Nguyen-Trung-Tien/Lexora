using Lexora.Domain.Common;

namespace Lexora.Domain.Entities;

public class ExampleSentence : Entity
{
    public Guid WordMeaningId { get; set; }
    public WordMeaning WordMeaning { get; set; } = null!;

    public string SentenceEn { get; set; } = string.Empty;
    public string SentenceVi { get; set; } = string.Empty;
    public string? AudioPath { get; set; }
}
