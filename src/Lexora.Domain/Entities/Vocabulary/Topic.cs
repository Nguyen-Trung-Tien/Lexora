using Lexora.Domain.Common;

namespace Lexora.Domain.Entities;

public class Topic : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconGlyph { get; set; }
    public string? ColorHex { get; set; }

    public ICollection<WordTopic> WordTopics { get; set; } = new List<WordTopic>();
}
