using Lexora.Domain.Common;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Entities;

public class Word : Entity
{
    public string Term { get; set; } = string.Empty;
    public string NormalizedTerm { get; set; } = string.Empty;
    public string? PhoneticUk { get; set; }
    public string? PhoneticUs { get; set; }
    public string? AudioUkPath { get; set; }
    public string? AudioUsPath { get; set; }
    public CefrLevel Level { get; set; } = CefrLevel.None;
    public PartOfSpeech PrimaryPartOfSpeech { get; set; } = PartOfSpeech.Noun;
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<WordMeaning> Meanings { get; set; } = new List<WordMeaning>();
    public ICollection<WordTopic> WordTopics { get; set; } = new List<WordTopic>();
    public ICollection<WordExamTag> WordExamTags { get; set; } = new List<WordExamTag>();
    public ReviewCard? ReviewCard { get; set; }
}
