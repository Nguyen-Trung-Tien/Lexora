using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Lexora.Presentation.Views;

public sealed partial class ReviewPage : Page
{
    public ReviewViewModel ViewModel { get; }
    public Lexora.Presentation.Services.ILocalizationService Loc { get; }

    private readonly IUserPreferencesService _preferencesService;

    public ReviewPage()
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = App.Current.Services.GetRequiredService<ReviewViewModel>();
        Loc = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.ILocalizationService>();
        _preferencesService = App.Current.Services.GetRequiredService<IUserPreferencesService>();

        InitializeComponent();

        IsTabStop = true;
        Loaded += (s, e) =>
        {
            UpdateSpeedFlyoutLabels();
            Focus(FocusState.Programmatic);
        };

        Loc.LanguageChanged += OnLanguageChanged;
        RegisterShortcuts();

        Unloaded += (s, e) =>
        {
            Loc.LanguageChanged -= OnLanguageChanged;
            ViewModel.StopAudio();
            var sm = App.Current.Services.GetService<IShortcutManager>();
            sm?.UnregisterPageShortcuts(typeof(ReviewPage));
        };
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.StopAudio();
    }


    private void UpdateSpeedFlyoutLabels()
    {
        if (FlyoutSpeedSlow != null) FlyoutSpeedSlow.Text = Loc.GetString("Settings_Speed_Slow");
        if (FlyoutSpeedNormal != null) FlyoutSpeedNormal.Text = Loc.GetString("Settings_Speed_Normal");
        if (FlyoutSpeedFast != null) FlyoutSpeedFast.Text = Loc.GetString("Settings_Speed_Fast");
        if (FlyoutSpeedVeryFast != null) FlyoutSpeedVeryFast.Text = Loc.GetString("Settings_Speed_VeryFast");
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateSpeedFlyoutLabels();
            ViewModel.UpdateActiveSessionTitle();
            Bindings.Update();
        });
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Focus(FocusState.Programmatic);

        try
        {
            if (e.Parameter is ContextUnitDto unit)
            {
                await ViewModel.LoadAvailableTopicsAsync();
                var prefs = _preferencesService.GetPreferences();
                ViewModel.IsTypingPracticeEnabled = prefs.EnableTypingPractice;
                ViewModel.SpeechRate = prefs.SpeechRate;
                UpdateModeButtonStyles(ReviewSessionMode.ByTopic);
                await ViewModel.StartUnitReviewAsync(unit);
            }
            else if (e.Parameter is IEnumerable<Guid> wordIds)
            {
                var idList = wordIds.ToList();
                await ViewModel.LoadAvailableTopicsAsync();
                var prefs = _preferencesService.GetPreferences();
                ViewModel.IsTypingPracticeEnabled = prefs.EnableTypingPractice;
                ViewModel.SpeechRate = prefs.SpeechRate;
                UpdateModeButtonStyles(ReviewSessionMode.CustomSelection);
                await ViewModel.StartCustomWordReviewAsync(idList);
            }
            else if (e.Parameter is TopicDto topic)
            {
                await ViewModel.LoadAvailableTopicsAsync();
                var prefs = _preferencesService.GetPreferences();
                ViewModel.IsTypingPracticeEnabled = prefs.EnableTypingPractice;
                ViewModel.SpeechRate = prefs.SpeechRate;
                UpdateModeButtonStyles(ReviewSessionMode.ByTopic);
                var topicTitle = string.Format(Loc.GetString("Review_Session_ByTopicFormat"), topic.Name);
                await ViewModel.StartReviewWithFilterAsync(ReviewSessionMode.ByTopic, topicId: topic.Id, customTitle: topicTitle);
            }
            else
            {
                if (ViewModel.CurrentCard == null && !ViewModel.IsCompleted)
                {
                    await ViewModel.InitializeAsync();
                }
                else
                {
                    _ = ViewModel.RefreshDueCountAsync();
                }
                UpdateModeButtonStyles(ViewModel.CurrentMode);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReviewPage] OnNavigatedTo error: {ex.Message}");
        }
    }



    private void RegisterShortcuts()
    {
        var sm = App.Current.Services.GetService<IShortcutManager>();
        if (sm == null) return;

        sm.RegisterPageShortcuts(typeof(ReviewPage), new[]
        {
            new ShortcutDefinition
            {
                Id = "Review.Undo",
                Gesture = new ShortcutGesture(ShortcutKey.Z, ShortcutModifiers.Control),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(ReviewPage),
                DescriptionKey = "Review_UndoTooltip",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.CanUndo && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = async () =>
                {
                    await ViewModel.UndoLastReviewAsync();
                }
            },
            new ShortcutDefinition
            {
                Id = "Review.ShowAnswer",
                Gesture = new ShortcutGesture(ShortcutKey.Space, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.Enter, ShortcutModifiers.None),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(ReviewPage),
                DescriptionKey = "Review_BtnShowAnswer",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.IsCardActive && !ViewModel.IsAnswerVisible && !ViewModel.IsTypingRecallActive && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = () =>
                {
                    ShowAnswerButton_Click(this, new RoutedEventArgs());
                    return Task.CompletedTask;
                }
            },
            new ShortcutDefinition
            {
                Id = "Review.RateAgain",
                Gesture = new ShortcutGesture(ShortcutKey.Number1, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.NumPad1, ShortcutModifiers.None),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(ReviewPage),
                DescriptionKey = "Review_RatingAgain",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.CanRate && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = async () =>
                {
                    await ViewModel.RateCardAsync(ReviewRating.Again);
                }
            },
            new ShortcutDefinition
            {
                Id = "Review.RateHard",
                Gesture = new ShortcutGesture(ShortcutKey.Number2, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.NumPad2, ShortcutModifiers.None),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(ReviewPage),
                DescriptionKey = "Review_RatingHard",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.CanRate && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = async () =>
                {
                    await ViewModel.RateCardAsync(ReviewRating.Hard);
                }
            },
            new ShortcutDefinition
            {
                Id = "Review.RateGood",
                Gesture = new ShortcutGesture(ShortcutKey.Number3, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.NumPad3, ShortcutModifiers.None),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(ReviewPage),
                DescriptionKey = "Review_RatingGood",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.CanRate && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = async () =>
                {
                    await ViewModel.RateCardAsync(ReviewRating.Good);
                }
            },
            new ShortcutDefinition
            {
                Id = "Review.RateEasy",
                Gesture = new ShortcutGesture(ShortcutKey.Number4, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.NumPad4, ShortcutModifiers.None),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(ReviewPage),
                DescriptionKey = "Review_RatingEasy",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.CanRate && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = async () =>
                {
                    await ViewModel.RateCardAsync(ReviewRating.Easy);
                }
            },
            new ShortcutDefinition
            {
                Id = "Review.PlayUkAudio",
                Gesture = new ShortcutGesture(ShortcutKey.U, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.U, ShortcutModifiers.Alt),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(ReviewPage),
                DescriptionKey = "Review_AudioUk",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.IsCardActive && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = async () =>
                {
                    await ViewModel.PlayUkAudioAsync();
                }
            },
            new ShortcutDefinition
            {
                Id = "Review.PlayUsAudio",
                Gesture = new ShortcutGesture(ShortcutKey.S, ShortcutModifiers.None),
                SecondaryGesture = new ShortcutGesture(ShortcutKey.S, ShortcutModifiers.Alt),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(ReviewPage),
                DescriptionKey = "Review_AudioUs",
                Priority = ShortcutPriority.Page,
                AllowWhenTextInputFocused = false,
                AllowWhenDialogActive = false,
                CanExecute = () => ViewModel.IsCardActive && !SafeDialogExtensions.IsDialogOpen,
                ExecuteAsync = async () =>
                {
                    await ViewModel.PlayUsAudioAsync();
                }
            }
        });
    }

    private void TypingInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            ViewModel.CheckTypedAnswer();
            e.Handled = true;
        }
    }

    private async void TypingModeToggle_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ToggleTypingPracticeAsync();
    }

    private async void SpeedItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string tagStr &&
            double.TryParse(tagStr, System.Globalization.CultureInfo.InvariantCulture, out double rate))
        {
            await ViewModel.SetSpeechRateAsync(rate);
        }
    }

    private void CheckTypedAnswerButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CheckTypedAnswer();
    }

    private void HintButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ProvideHint();
    }

    private void SkipTypingButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SkipTyping();
    }

    private async void ModeDueButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateModeButtonStyles(ReviewSessionMode.TodayDue);
        await ViewModel.StartReviewAsync(ReviewSessionMode.TodayDue);
    }

    private async void ModeNewButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateModeButtonStyles(ReviewSessionMode.LearnNew);
        await ViewModel.StartReviewAsync(ReviewSessionMode.LearnNew);
    }

    private async void ModeDifficultButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateModeButtonStyles(ReviewSessionMode.Difficult);
        await ViewModel.StartReviewAsync(ReviewSessionMode.Difficult);
    }

    private async void ReviewUnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReviewUnitCombo?.SelectedItem is TopicDto selectedTopic)
        {
            UpdateModeButtonStyles(ReviewSessionMode.ByTopic);
            var topicTitle = string.Format(Loc.GetString("Review_Session_ByTopicFormat"), selectedTopic.Name);
            await ViewModel.StartReviewWithFilterAsync(
                ReviewSessionMode.ByTopic,
                topicId: selectedTopic.Id,
                customTitle: topicTitle);
        }
    }

    private void UpdateModeButtonStyles(ReviewSessionMode mode)
    {
        var accentStyle = App.Current.Resources.TryGetValue("AccentButtonStyle", out var a) ? (Style)a : null;
        var defaultStyle = App.Current.Resources.TryGetValue("DefaultButtonStyle", out var d) ? (Style)d : null;

        ModeDueButton.Style = mode == ReviewSessionMode.TodayDue ? accentStyle : defaultStyle;
        ModeNewButton.Style = mode == ReviewSessionMode.LearnNew ? accentStyle : defaultStyle;
        ModeDifficultButton.Style = mode == ReviewSessionMode.Difficult ? accentStyle : defaultStyle;

        if (mode != ReviewSessionMode.ByTopic && ReviewUnitCombo != null)
        {
            ReviewUnitCombo.SelectedItem = null;
        }
    }

    private void ShowAnswerButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RevealAnswer();
        if (_preferencesService.GetPreferences().AutoPlayAudio)
        {
            _ = ViewModel.PlayPreferredAudioAsync();
        }
    }

    private async void AgainButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RateCardAsync(ReviewRating.Again);
    }

    private async void HardButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RateCardAsync(ReviewRating.Hard);
    }

    private async void GoodButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RateCardAsync(ReviewRating.Good);
    }

    private async void EasyButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RateCardAsync(ReviewRating.Easy);
    }

    private async void PlayUkAudio_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PlayUkAudioAsync();
    }

    private async void PlayUsAudio_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PlayUsAudioAsync();
    }

    private async void RestartReviewButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.StartReviewAsync(ViewModel.CurrentMode);
    }
}
