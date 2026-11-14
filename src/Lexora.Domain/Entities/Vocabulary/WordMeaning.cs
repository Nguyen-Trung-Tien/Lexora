using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities;

public class WordMeaning : Entity
{
    public Guid WordId { get; set; }
    public Word Word { get; set; } = null!;

    public PartOfSpeech PartOfSpeech { get; set; } = PartOfSpeech.Noun;
    public string DefinitionVi { get; set; } = string.Empty;
    public string? DefinitionEn { get; set; }
    public string? Explanation { get; set; }
    public string? ContextNotes { get; set; }
    public int OrderIndex { get; set; } = 0;

    public ICollection<ExampleSentence> Examples { get; set; } = new List<ExampleSentence>();
}
