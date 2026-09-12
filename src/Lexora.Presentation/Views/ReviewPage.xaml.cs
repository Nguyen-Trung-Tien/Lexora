using Lexora.Application.DTOs;
using Lexora.Domain.Enums;
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

    private readonly Lexora.Infrastructure.Services.IUserPreferencesService _preferencesService;

    public ReviewPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<ReviewViewModel>();
        _preferencesService = App.Current.Services.GetRequiredService<Lexora.Infrastructure.Services.IUserPreferencesService>();

        IsTabStop = true;
        Loaded += async (s, e) =>
        {
            Focus(FocusState.Programmatic);
            await ViewModel.InitializeAsync();
            UpdateModeButtonStyles(ViewModel.CurrentMode);
        };

        KeyDown += ReviewPage_KeyDown;
    }

    private void ReviewPage_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (ViewModel.IsCompleted || ViewModel.IsLoading) return;

        if (e.Key == VirtualKey.Space && !ViewModel.IsAnswerVisible)
        {
            ViewModel.RevealAnswer();
            if (_preferencesService.GetPreferences().AutoPlayAudio)
            {
                _ = ViewModel.PlayUkAudioAsync();
            }
            e.Handled = true;
        }
        else if (ViewModel.IsAnswerVisible)
        {
            switch (e.Key)
            {
                case VirtualKey.Number1 or VirtualKey.NumberPad1:
                    _ = ViewModel.RateCardAsync(ReviewRating.Again);
                    e.Handled = true;
                    break;
                case VirtualKey.Number2 or VirtualKey.NumberPad2:
                    _ = ViewModel.RateCardAsync(ReviewRating.Hard);
                    e.Handled = true;
                    break;
                case VirtualKey.Number3 or VirtualKey.NumberPad3:
                    _ = ViewModel.RateCardAsync(ReviewRating.Good);
                    e.Handled = true;
                    break;
                case VirtualKey.Number4 or VirtualKey.NumberPad4:
                    _ = ViewModel.RateCardAsync(ReviewRating.Easy);
                    e.Handled = true;
                    break;
            }
        }

        if (e.Key == VirtualKey.U)
        {
            _ = ViewModel.PlayUkAudioAsync();
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.S)
        {
            _ = ViewModel.PlayUsAudioAsync();
            e.Handled = true;
        }
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

    private void UpdateModeButtonStyles(ReviewSessionMode mode)
    {
        var accentStyle = Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("AccentButtonStyle", out var a) ? (Style)a : null;
        var defaultStyle = Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("DefaultButtonStyle", out var d) ? (Style)d : null;

        ModeDueButton.Style = mode == ReviewSessionMode.TodayDue ? accentStyle : defaultStyle;
        ModeNewButton.Style = mode == ReviewSessionMode.LearnNew ? accentStyle : defaultStyle;
        ModeDifficultButton.Style = mode == ReviewSessionMode.Difficult ? accentStyle : defaultStyle;
    }

    private void ShowAnswerButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RevealAnswer();
        if (_preferencesService.GetPreferences().AutoPlayAudio)
        {
            _ = ViewModel.PlayUkAudioAsync();
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
