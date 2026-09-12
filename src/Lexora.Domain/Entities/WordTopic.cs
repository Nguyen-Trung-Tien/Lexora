namespace Lexora.Domain.Entities;

public class WordTopic
{
    public Guid WordId { get; set; }
    public Word Word { get; set; } = null!;

    public Guid TopicId { get; set; }
    public Topic Topic { get; set; } = null!;
}
