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

    public TestPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<TestViewModel>();
        KeyDown += TestPage_KeyDown;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        UpdateTypeButtonStyles(ViewModel.SelectedTestType);
        UpdateCountButtonStyles(ViewModel.QuestionCount);
        await ViewModel.InitializeAsync();
    }

    private async void TestPage_KeyDown(object sender, KeyRoutedEventArgs e)
    {
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
        var accent = (Style)Microsoft.UI.Xaml.Application.Current.Resources["AccentButtonStyle"];
        var def = (Style)Microsoft.UI.Xaml.Application.Current.Resources["DefaultButtonStyle"];

        BtnTypeMixed.Style = selected == TestType.Mixed ? accent : def;
        BtnTypeWordToMeaning.Style = selected == TestType.WordToMeaning ? accent : def;
        BtnTypeMeaningToWord.Style = selected == TestType.MeaningToWord ? accent : def;
        BtnTypeFillInTheBlank.Style = selected == TestType.FillInTheBlank ? accent : def;
        BtnTypePartOfSpeech.Style = selected == TestType.PartOfSpeech ? accent : def;
        BtnTypeListeningQuiz.Style = selected == TestType.ListeningQuiz ? accent : def;
    }

    private void UpdateCountButtonStyles(int count)
    {
        var accent = (Style)Microsoft.UI.Xaml.Application.Current.Resources["AccentButtonStyle"];
        var def = (Style)Microsoft.UI.Xaml.Application.Current.Resources["DefaultButtonStyle"];

        BtnCount5.Style = count == 5 ? accent : def;
        BtnCount10.Style = count == 10 ? accent : def;
        BtnCount20.Style = count == 20 ? accent : def;
        BtnCount30.Style = count == 30 ? accent : def;
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
