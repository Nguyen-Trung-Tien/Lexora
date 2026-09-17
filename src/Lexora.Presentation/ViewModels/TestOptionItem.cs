using CommunityToolkit.Mvvm.ComponentModel;

namespace Lexora.Presentation.ViewModels;

public enum TestOptionState
{
    Default,
    SelectedCorrect,
    SelectedIncorrect,
    RevealedCorrect
}

public partial class TestOptionItem : ObservableObject
{
    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TestOptionState State { get; set; } = TestOptionState.Default;

    [ObservableProperty]
    public partial string Glyph { get; set; } = "\uE739";
}
