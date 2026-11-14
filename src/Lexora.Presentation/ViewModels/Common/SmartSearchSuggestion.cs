namespace Lexora.Presentation.ViewModels;

public sealed class SmartSearchSuggestion
{
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string Glyph { get; init; }
    public string? TargetPage { get; init; }
    public string? SearchTerm { get; init; }
    public string? TargetType { get; init; }

    public override string ToString() => Title;
}
