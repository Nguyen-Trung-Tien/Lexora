using CommunityToolkit.Mvvm.ComponentModel;

namespace Lexora.Presentation.ViewModels;

public enum TestOptionState
{
    Default,
    Selected,
    SelectedCorrect,
    SelectedIncorrect,
    RevealedCorrect
}

public partial class TestOptionItem : ObservableObject
{
    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnswered))]
    [NotifyPropertyChangedFor(nameof(IsSelectable))]
    [NotifyPropertyChangedFor(nameof(ShowLetter))]
    public partial TestOptionState State { get; set; } = TestOptionState.Default;

    [ObservableProperty]
    public partial string Glyph { get; set; } = "\uE739";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowLetter))]
    public partial string Letter { get; set; } = string.Empty;

    public bool IsAnswered => State == TestOptionState.SelectedCorrect ||
                              State == TestOptionState.SelectedIncorrect ||
                              State == TestOptionState.RevealedCorrect;

    public bool IsSelectable => !IsAnswered;

    public bool ShowLetter => !IsAnswered && !string.IsNullOrEmpty(Letter);
}
