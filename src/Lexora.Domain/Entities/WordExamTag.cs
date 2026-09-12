namespace Lexora.Domain.Entities;

public class WordExamTag
{
    public Guid WordId { get; set; }
    public Word Word { get; set; } = null!;

    public Guid ExamTagId { get; set; }
    public ExamTag ExamTag { get; set; } = null!;
}
