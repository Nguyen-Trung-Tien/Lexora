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

        ActualThemeChanged += (s, e) =>
        {
            UpdateTypeButtonStyles(ViewModel.SelectedTestType);
            UpdateCountButtonStyles(ViewModel.QuestionCount);
            UpdateModeSelectorStyles(ViewModel.IsCustomListMode);
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            Bindings.Update();
            UpdateCountButtonLabels();
        });
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        try
        {
            UpdateCountButtonLabels();
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TestPage] OnNavigatedTo error: {ex.Message}");
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
            ViewModel.SelectOption(ViewModel.CurrentOptions[index].Text);
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
        try
        {
            await ViewModel.SwitchToCustomListModeAsync();
            UpdateModeSelectorStyles(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TestPage] ModeCustomList error: {ex.Message}");
        }
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

    private void UpdateCountButtonLabels()
    {
        BtnCount5.Content = Loc.GetString("Test_Count5");
        BtnCount10.Content = Loc.GetString("Test_Count10");
        BtnCount20.Content = Loc.GetString("Test_Count20");
        BtnCount30.Content = Loc.GetString("Test_Count30");
    }

    private Microsoft.UI.Xaml.Media.Brush? GetThemeBrush(string key)
    {
        return Services.ThemeResourceHelper.GetThemeBrush(this, key);
    }

    private void UpdateModeSelectorStyles(bool isCustomList)
    {
        var cardBg = GetThemeBrush("CardBackgroundFillColorDefaultBrush");
        var cardStroke = GetThemeBrush("CardStrokeColorDefaultBrush");
        var accentText = GetThemeBrush("AccentTextFillColorPrimaryBrush");
        var subtleText = GetThemeBrush("TextFillColorSecondaryBrush");
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        void ApplySegment(Button btn, bool isActive)
        {
            btn.Style = null;
            if (isActive)
            {
                if (cardBg != null) btn.Background = cardBg;
                if (cardStroke != null) btn.BorderBrush = cardStroke;
                btn.BorderThickness = new Thickness(1);
                if (accentText != null) btn.Foreground = accentText;
                btn.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            }
            else
            {
                btn.Background = transparent;
                btn.BorderThickness = new Thickness(0);
                if (subtleText != null) btn.Foreground = subtleText;
                btn.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
            }
        }

        ApplySegment(BtnModeFilters, !isCustomList);
        ApplySegment(BtnModeCustomList, isCustomList);
    }

    private async void WeakWordAudio_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.Tag is WordDto word)
            {
                await ViewModel.PlayWordAudioAsync(word);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TestPage] WeakWordAudio error: {ex.Message}");
        }
    }

    private void UpdateTypeButtonStyles(TestType selected)
    {
        var accentBrush = GetThemeBrush("AccentFillColorDefaultBrush");
        var cardStroke = GetThemeBrush("CardStrokeColorDefaultBrush");
        var subtleBg = GetThemeBrush("SubtleFillColorSecondaryBrush");
        var cardBg = GetThemeBrush("CardBackgroundFillColorDefaultBrush");
        var accentText = GetThemeBrush("AccentTextFillColorPrimaryBrush");
        var primaryText = GetThemeBrush("TextFillColorPrimaryBrush");

        void ApplyCard(Button btn, FontIcon icon, TextBlock title, Border checkBadge, bool isSelected)
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
                checkBadge.Visibility = Visibility.Visible;
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
                checkBadge.Visibility = Visibility.Collapsed;
            }
        }

        ApplyCard(BtnTypeMixed, IconTypeMixed, TitleTypeMixed, CheckBadgeTypeMixed, selected == TestType.Mixed);
        ApplyCard(BtnTypeWordToMeaning, IconTypeWordToMeaning, TitleTypeWordToMeaning, CheckBadgeTypeWordToMeaning, selected == TestType.WordToMeaning);
        ApplyCard(BtnTypeMeaningToWord, IconTypeMeaningToWord, TitleTypeMeaningToWord, CheckBadgeTypeMeaningToWord, selected == TestType.MeaningToWord);
        ApplyCard(BtnTypeFillInTheBlank, IconTypeFillInTheBlank, TitleTypeFillInTheBlank, CheckBadgeTypeFillInTheBlank, selected == TestType.FillInTheBlank);
        ApplyCard(BtnTypePartOfSpeech, IconTypePartOfSpeech, TitleTypePartOfSpeech, CheckBadgeTypePartOfSpeech, selected == TestType.PartOfSpeech);
        ApplyCard(BtnTypeListeningQuiz, IconTypeListeningQuiz, TitleTypeListeningQuiz, CheckBadgeTypeListeningQuiz, selected == TestType.ListeningQuiz);
    }

    private void UpdateCountButtonStyles(int count)
    {
        var cardBg = GetThemeBrush("CardBackgroundFillColorDefaultBrush");
        var cardStroke = GetThemeBrush("CardStrokeColorDefaultBrush");
        var accentText = GetThemeBrush("AccentTextFillColorPrimaryBrush");
        var subtleText = GetThemeBrush("TextFillColorSecondaryBrush");
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        void ApplyPill(Button btn, bool isSelected)
        {
            btn.Style = null;
            if (isSelected)
            {
                if (cardBg != null) btn.Background = cardBg;
                if (cardStroke != null) btn.BorderBrush = cardStroke;
                btn.BorderThickness = new Thickness(1);
                if (accentText != null) btn.Foreground = accentText;
                btn.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            }
            else
            {
                btn.Background = transparent;
                btn.BorderThickness = new Thickness(0);
                if (subtleText != null) btn.Foreground = subtleText;
                btn.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
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
        return isCorrect ? Loc.GetString("Grammar_Correct") : Loc.GetString("Grammar_Incorrect");
    }
}
