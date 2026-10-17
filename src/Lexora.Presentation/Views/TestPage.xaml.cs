using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace Lexora.Presentation.Views;

public sealed partial class TestPage : Page, ILocalizableView
{
    public TestViewModel ViewModel { get; }
    public Lexora.Presentation.Services.ILocalizationService Loc { get; }

    public TestPage()
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = App.Current.Services.GetRequiredService<TestViewModel>();
        Loc = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.ILocalizationService>();

        InitializeComponent();
        RegisterShortcuts();

        Unloaded += (s, e) =>
        {
            ViewModel.CleanupSession();
            var sm = App.Current.Services.GetService<IShortcutManager>();
            sm?.UnregisterPageShortcuts(typeof(TestPage));
        };

        ActualThemeChanged += (s, e) =>
        {
            UpdateTypeButtonStyles(ViewModel.SelectedTestType);
            UpdateCountButtonStyles(ViewModel.QuestionCount);
            UpdateModeSelectorStyles(ViewModel.IsCustomListMode);
        };
    }

    public void OnLanguageChanged()
    {
        void UpdateUI()
        {
            ViewModel.UpdateLanguageState();
            Bindings.Update();
            UpdateCountButtonLabels();
        }

        if (DispatcherQueue.HasThreadAccess)
        {
            UpdateUI();
        }
        else
        {
            DispatcherQueue.TryEnqueue(UpdateUI);
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        OnLanguageChanged();
        try
        {
            RegisterShortcuts();
            UpdateCountButtonLabels();
            UpdateTypeButtonStyles(ViewModel.SelectedTestType);
            UpdateCountButtonStyles(ViewModel.QuestionCount);
            if (ViewModel.Topics.Count == 0)
            {
                await ViewModel.InitializeAsync();
            }

            if (e.Parameter is TestSessionDto session)
            {
                ViewModel.StartTestWithSession(session);
            }
            else if (e.Parameter is IEnumerable<Guid> wordIds)
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

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.CleanupSession();
    }

    private void RegisterShortcuts()
    {
        var sm = App.Current.Services.GetService<IShortcutManager>();
        if (sm == null) return;

        sm.RegisterPageShortcuts(typeof(TestPage), new[]
        {
            new ShortcutDefinition
            {
                Id = "Test.PlayAudio",
                Gesture = new ShortcutGesture(ShortcutKey.Space, ShortcutModifiers.None),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(TestPage),
                DescriptionKey = "Review_AudioUk",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.IsTestingMode && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = async () =>
                {
                    await ViewModel.PlayAudioAsync();
                }
            },
            new ShortcutDefinition
            {
                Id = "Test.NextQuestion",
                Gesture = new ShortcutGesture(ShortcutKey.Enter, ShortcutModifiers.None),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(TestPage),
                DescriptionKey = "Common_Next",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.IsTestingMode && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = async () =>
                {
                    await ViewModel.NextQuestionAsync();
                }
            },
            new ShortcutDefinition
            {
                Id = "Test.SelectOption1",
                Gesture = new ShortcutGesture(ShortcutKey.Number1, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.A, ShortcutModifiers.None),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(TestPage),
                DescriptionKey = "Test_Option1",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.IsTestingMode && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = () =>
                {
                    SelectOptionByIndex(0);
                    return Task.CompletedTask;
                }
            },
            new ShortcutDefinition
            {
                Id = "Test.SelectOption2",
                Gesture = new ShortcutGesture(ShortcutKey.Number2, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.B, ShortcutModifiers.None),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(TestPage),
                DescriptionKey = "Test_Option2",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.IsTestingMode && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = () =>
                {
                    SelectOptionByIndex(1);
                    return Task.CompletedTask;
                }
            },
            new ShortcutDefinition
            {
                Id = "Test.SelectOption3",
                Gesture = new ShortcutGesture(ShortcutKey.Number3, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.C, ShortcutModifiers.None),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(TestPage),
                DescriptionKey = "Test_Option3",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.IsTestingMode && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = () =>
                {
                    SelectOptionByIndex(2);
                    return Task.CompletedTask;
                }
            },
            new ShortcutDefinition
            {
                Id = "Test.SelectOption4",
                Gesture = new ShortcutGesture(ShortcutKey.Number4, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.D, ShortcutModifiers.None),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(TestPage),
                DescriptionKey = "Test_Option4",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.IsTestingMode && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = () =>
                {
                    SelectOptionByIndex(3);
                    return Task.CompletedTask;
                }
            }
        });
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

    private void StatusInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        ViewModel.StatusMessage = string.Empty;
    }
}
