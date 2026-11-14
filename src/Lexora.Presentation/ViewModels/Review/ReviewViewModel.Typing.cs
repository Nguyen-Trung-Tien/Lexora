using CommunityToolkit.Mvvm.Input;

namespace Lexora.Presentation.ViewModels;

public partial class ReviewViewModel
{
    [RelayCommand]
    public async Task ToggleTypingPracticeAsync()
    {
        IsTypingPracticeEnabled = !IsTypingPracticeEnabled;
        var current = _preferencesService.GetPreferences();
        await _preferencesService.SavePreferencesAsync(current with { EnableTypingPractice = IsTypingPracticeEnabled });
    }

    [RelayCommand]
    public void CheckTypedAnswer()
    {
        if (CurrentCard == null || string.IsNullOrWhiteSpace(TypedWord)) return;

        var expected = CurrentCard.Term.Trim();
        var actual = TypedWord.Trim();

        bool isCorrect = string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
        IsTypingCorrect = isCorrect;

        if (isCorrect)
        {
            TypingFeedbackMessage = _localizationService.GetString("Review_TypingFeedbackCorrect");
            IsAnswerVisible = true;
            _ = PlayPreferredAudioAsync();
        }
        else
        {
            TypingFeedbackMessage = string.Format(_localizationService.GetString("Review_TypingFeedbackIncorrectFormat"), actual, CurrentCard.Term);
            IsAnswerVisible = true;
        }
    }

    [RelayCommand]
    public void ProvideHint()
    {
        if (CurrentCard == null) return;
        var term = CurrentCard.Term.Trim();
        if (term.Length <= 2)
        {
            HintText = term;
        }
        else
        {
            char first = term[0];
            char last = term[^1];
            string dashes = string.Join(" ", Enumerable.Repeat("_", term.Length - 2));
            HintText = $"{first} {dashes} {last}";
        }
        IsHintVisible = true;
    }

    [RelayCommand]
    public void SkipTyping()
    {
        IsAnswerVisible = true;
    }
}
