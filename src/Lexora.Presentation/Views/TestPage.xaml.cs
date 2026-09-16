using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace Lexora.Presentation.Views;

public sealed partial class TestPage : Page
{
    public TestViewModel ViewModel { get; }
    public Lexora.Presentation.Services.ILocalizationService Loc { get; }

    public TestPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<TestViewModel>();
        Loc = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.ILocalizationService>();
        KeyDown += TestPage_KeyDown;

        Loc.LanguageChanged += OnLanguageChanged;
        Unloaded += (s, e) => Loc.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => Bindings.Update());
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        UpdateTypeButtonStyles(ViewModel.SelectedTestType);
        UpdateCountButtonStyles(ViewModel.QuestionCount);
        await ViewModel.InitializeAsync();

        if (e.Parameter is IEnumerable<Guid> wordIds)
        {
            await ViewModel.SetCustomWordSelectionAsync(wordIds);
            UpdateModeSelectorStyles(true);
        }
        else
        {
            UpdateModeSelectorStyles(ViewModel.IsCustomListMode);
        }
    }

    private async void TestPage_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Guard against hotkeys when typing in a text box
        if (FocusManager.GetFocusedElement(this.XamlRoot) is TextBox) return;

        if (ViewModel.IsTestingMode)
        {
            if (e.Key == VirtualKey.Space)
            {
                e.Handled = true;
                await ViewModel.PlayAudioAsync();
            }
            else if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                await ViewModel.NextQuestionAsync();
            }
            else if (e.Key is VirtualKey.Number1 or VirtualKey.A)
            {
                SelectOptionByIndex(0);
                e.Handled = true;
            }
            else if (e.Key is VirtualKey.Number2 or VirtualKey.B)
            {
                SelectOptionByIndex(1);
                e.Handled = true;
            }
            else if (e.Key is VirtualKey.Number3 or VirtualKey.C)
            {
                SelectOptionByIndex(2);
                e.Handled = true;
            }
            else if (e.Key is VirtualKey.Number4 or VirtualKey.D)
            {
                SelectOptionByIndex(3);
                e.Handled = true;
            }
        }
    }

    private void SelectOptionByIndex(int index)
    {
        if (index >= 0 && index < ViewModel.CurrentOptions.Count)
        {
            ViewModel.SelectOption(ViewModel.CurrentOptions[index]);
        }
    }

    private void TestTypeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr && Enum.TryParse<TestType>(tagStr, out var type))
        {
            ViewModel.SelectedTestType = type;
            UpdateTypeButtonStyles(type);
        }
    }

    private void QuestionCountButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string countStr && int.TryParse(countStr, out var count))
        {
            ViewModel.QuestionCount = count;
            UpdateCountButtonStyles(count);
        }
    }

    private void OptionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string option)
        {
            ViewModel.SelectOption(option);
        }
    }

    private void ExitTestButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RestartSetup();
        UpdateTypeButtonStyles(ViewModel.SelectedTestType);
        UpdateCountButtonStyles(ViewModel.QuestionCount);
        UpdateModeSelectorStyles(ViewModel.IsCustomListMode);
    }

    private void ModeFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SwitchToFilterMode();
        UpdateModeSelectorStyles(false);
    }

    private async void ModeCustomListButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SwitchToCustomListModeAsync();
        UpdateModeSelectorStyles(true);
    }

    private void CustomWordSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.ApplyCustomWordFilter();
    }

    private void SelectAllCustomWords_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAllCustomWords();
    }

    private void DeselectAllCustomWords_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeselectAllCustomWords();
    }

    private void SelectWeakCustomWords_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectWeakCustomWords();
    }

    private void ApplyPastedTermsButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyPastedTerms();
    }

    private void UpdateModeSelectorStyles(bool isCustomList)
    {
        var accentStyle = Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("AccentButtonStyle", out var a) ? (Style)a : null;
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        var subtleText = Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("TextFillColorSecondaryBrush", out var st) && st is Microsoft.UI.Xaml.Media.Brush bSt ? bSt : null;

        if (isCustomList)
        {
            BtnModeFilters.Style = null;
            BtnModeFilters.Background = transparent;
            BtnModeFilters.BorderThickness = new Thickness(0);
            if (subtleText != null) BtnModeFilters.Foreground = subtleText;

            BtnModeCustomList.Style = accentStyle;
        }
        else
        {
            BtnModeFilters.Style = accentStyle;

            BtnModeCustomList.Style = null;
            BtnModeCustomList.Background = transparent;
            BtnModeCustomList.BorderThickness = new Thickness(0);
            if (subtleText != null) BtnModeCustomList.Foreground = subtleText;
        }
    }

    private async void WeakWordAudio_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is WordDto word)
        {
            await ViewModel.PlayWordAudioAsync(word);
        }
    }

    private void UpdateTypeButtonStyles(TestType selected)
    {
        var res = Microsoft.UI.Xaml.Application.Current.Resources;
        var accentBrush = res.TryGetValue("AccentFillColorDefaultBrush", out var ab) && ab is Microsoft.UI.Xaml.Media.Brush bAb ? bAb : null;
        var cardStroke = res.TryGetValue("CardStrokeColorDefaultBrush", out var cs) && cs is Microsoft.UI.Xaml.Media.Brush bCs ? bCs : null;
        var subtleBg = res.TryGetValue("SubtleFillColorSecondaryBrush", out var sb) && sb is Microsoft.UI.Xaml.Media.Brush bSb ? bSb : null;
        var cardBg = res.TryGetValue("CardBackgroundFillColorDefaultBrush", out var cb) && cb is Microsoft.UI.Xaml.Media.Brush bCb ? bCb : null;
        var accentText = res.TryGetValue("AccentTextFillColorPrimaryBrush", out var at) && at is Microsoft.UI.Xaml.Media.Brush bAt ? bAt : null;
        var primaryText = res.TryGetValue("TextFillColorPrimaryBrush", out var pt) && pt is Microsoft.UI.Xaml.Media.Brush bPt ? bPt : null;

        void ApplyCard(Button btn, FontIcon icon, TextBlock title, FontIcon check, bool isSelected)
        {
            btn.BorderThickness = new Thickness(isSelected ? 2 : 1);
            if (isSelected)
            {
                if (accentBrush != null) btn.BorderBrush = accentBrush;
                if (subtleBg != null) btn.Background = subtleBg;
                if (accentText != null)
                {
                    icon.Foreground = accentText;
                    title.Foreground = accentText;
                }
                check.Visibility = Visibility.Visible;
            }
            else
            {
                if (cardStroke != null) btn.BorderBrush = cardStroke;
                if (cardBg != null) btn.Background = cardBg;
                if (primaryText != null)
                {
                    icon.Foreground = primaryText;
                    title.Foreground = primaryText;
                }
                check.Visibility = Visibility.Collapsed;
            }
        }

        ApplyCard(BtnTypeMixed, IconTypeMixed, TitleTypeMixed, CheckTypeMixed, selected == TestType.Mixed);
        ApplyCard(BtnTypeWordToMeaning, IconTypeWordToMeaning, TitleTypeWordToMeaning, CheckTypeWordToMeaning, selected == TestType.WordToMeaning);
        ApplyCard(BtnTypeMeaningToWord, IconTypeMeaningToWord, TitleTypeMeaningToWord, CheckTypeMeaningToWord, selected == TestType.MeaningToWord);
        ApplyCard(BtnTypeFillInTheBlank, IconTypeFillInTheBlank, TitleTypeFillInTheBlank, CheckTypeFillInTheBlank, selected == TestType.FillInTheBlank);
        ApplyCard(BtnTypePartOfSpeech, IconTypePartOfSpeech, TitleTypePartOfSpeech, CheckTypePartOfSpeech, selected == TestType.PartOfSpeech);
        ApplyCard(BtnTypeListeningQuiz, IconTypeListeningQuiz, TitleTypeListeningQuiz, CheckTypeListeningQuiz, selected == TestType.ListeningQuiz);
    }

    private void UpdateCountButtonStyles(int count)
    {
        var accent = Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("AccentButtonStyle", out var a) ? (Style)a : null;
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        var subtleText = Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("TextFillColorSecondaryBrush", out var st) && st is Microsoft.UI.Xaml.Media.Brush bSt ? bSt : null;

        void ApplyPill(Button btn, bool isSelected)
        {
            if (isSelected)
            {
                btn.Style = accent;
            }
            else
            {
                btn.Style = null;
                btn.Background = transparent;
                btn.BorderThickness = new Thickness(0);
                if (subtleText != null) btn.Foreground = subtleText;
            }
        }

        ApplyPill(BtnCount5, count == 5);
        ApplyPill(BtnCount10, count == 10);
        ApplyPill(BtnCount20, count == 20);
        ApplyPill(BtnCount30, count == 30);
    }

    public InfoBarSeverity GetFeedbackSeverity(bool isCorrect)
    {
        return isCorrect ? InfoBarSeverity.Success : InfoBarSeverity.Error;
    }

    public string GetFeedbackTitle(bool isCorrect)
    {
        return isCorrect ? "Correct! Well done." : "Incorrect.";
    }
}
