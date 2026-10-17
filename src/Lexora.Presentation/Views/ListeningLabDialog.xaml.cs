using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Lexora.Presentation.Views;

public sealed partial class ListeningLabDialog : ContentDialog, INotifyPropertyChanged
{
    private readonly IPronunciationListeningLabService _labService;
    private readonly IAudioPlayerService? _audioService;

    public ILocalizationService Loc { get; }

    private ListeningDictationChallengeDto? _currentDictation;
    public ListeningDictationChallengeDto? CurrentDictation
    {
        get => _currentDictation;
        set
        {
            if (_currentDictation != value)
            {
                _currentDictation = value;
                OnPropertyChanged();
            }
        }
    }

    private MinimalPairChallengeDto? _currentPair;
    public MinimalPairChallengeDto? CurrentPair
    {
        get => _currentPair;
        set
        {
            if (_currentPair != value)
            {
                _currentPair = value;
                OnPropertyChanged();
            }
        }
    }

    private IReadOnlyList<AccentComparisonItemDto> _accentComparisonList = [];
    public IReadOnlyList<AccentComparisonItemDto> AccentComparisonList
    {
        get => _accentComparisonList;
        set
        {
            if (_accentComparisonList != value)
            {
                _accentComparisonList = value;
                OnPropertyChanged();
            }
        }
    }

    private IReadOnlyList<MinimalPairChallengeDto> _minimalPairsQueue = [];
    private int _pairIndex;
    private double _playbackRate = 1.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ListeningLabDialog(
        IPronunciationListeningLabService labService,
        ILocalizationService loc,
        IAudioPlayerService? audioService)
    {
        _labService = labService;
        Loc = loc;
        _audioService = audioService;

        InitializeComponent();
        Loaded += ListeningLabDialog_Loaded;
    }

    private async void ListeningLabDialog_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadNextDictationAsync();
        await LoadMinimalPairsAsync();
        AccentComparisonList = await _labService.GetAccentComparisonListAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TAB 1: Dictation Lab
    // ─────────────────────────────────────────────────────────────────────────

    private async Task LoadNextDictationAsync()
    {
        DictationInputBox.Text = string.Empty;
        DictationResultPanel.Visibility = Visibility.Collapsed;
        CurrentDictation = await _labService.GetNextDictationChallengeAsync();
        if (CurrentDictation != null && _audioService != null)
        {
            await _audioService.PlayPronunciationAsync(CurrentDictation.Term, "uk", _playbackRate);
        }
    }

    private async void PlayUkAudio_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDictation == null || _audioService == null) return;
        await _audioService.PlayPronunciationAsync(CurrentDictation.Term, "uk", _playbackRate);
    }

    private async void PlayUsAudio_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDictation == null || _audioService == null) return;
        await _audioService.PlayPronunciationAsync(CurrentDictation.Term, "us", _playbackRate);
    }

    private void SpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SpeedComboBox?.SelectedItem is ComboBoxItem item &&
            double.TryParse(item.Tag?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double speed))
        {
            _playbackRate = speed;
        }
    }

    private void DictationInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            EvaluateCurrentDictation();
        }
    }

    private void CheckDictation_Click(object sender, RoutedEventArgs e)
    {
        EvaluateCurrentDictation();
    }

    private void EvaluateCurrentDictation()
    {
        if (CurrentDictation == null) return;

        var result = _labService.EvaluateDictation(
            DictationInputBox.Text,
            CurrentDictation.Term,
            CurrentDictation.PhoneticUk ?? CurrentDictation.PhoneticUs);

        DictationFeedbackText.Text = result.FeedbackMessage;
        DictationAccuracyText.Text = $"{result.AccuracyPercent}%";

        if (result.IsExactMatch)
        {
            DictationResultIcon.Glyph = "\uE73E"; // Checkmark
            DictationResultIcon.Foreground = (Brush)App.Current.Resources["SystemFillColorSuccessBrush"];
        }
        else if (result.LevenshteinDistance <= 2)
        {
            DictationResultIcon.Glyph = "\uE7BA"; // Warning
            DictationResultIcon.Foreground = (Brush)App.Current.Resources["SystemFillColorCautionBrush"];
        }
        else
        {
            DictationResultIcon.Glyph = "\uE711"; // Cross
            DictationResultIcon.Foreground = (Brush)App.Current.Resources["SystemFillColorCriticalBrush"];
        }

        DictationRevealedTerm.Text = CurrentDictation.Term;
        DictationRevealedPhonetic.Text = CurrentDictation.PhoneticUk ?? CurrentDictation.PhoneticUs ?? string.Empty;
        DictationRevealedMeaning.Text = CurrentDictation.DefinitionVi;
        DictationRevealedExample.Text = CurrentDictation.ExampleSentenceEn ?? string.Empty;

        DictationResultPanel.Visibility = Visibility.Visible;
    }

    private async void NextDictation_Click(object sender, RoutedEventArgs e)
    {
        await LoadNextDictationAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TAB 2: Minimal Pairs
    // ─────────────────────────────────────────────────────────────────────────

    private async Task LoadMinimalPairsAsync()
    {
        _minimalPairsQueue = await _labService.GetMinimalPairChallengesAsync(8);
        _pairIndex = 0;
        DisplayCurrentPair();
    }

    private void DisplayCurrentPair()
    {
        PairFeedbackPanel.Visibility = Visibility.Collapsed;
        ResetChoiceButtonsStyle();

        if (_minimalPairsQueue.Count > 0)
        {
            CurrentPair = _minimalPairsQueue[_pairIndex % _minimalPairsQueue.Count];
        }
    }

    private async void PlayPairPrompt_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentPair == null || _audioService == null) return;
        await _audioService.PlayPronunciationAsync(CurrentPair.TargetWord, "uk", _playbackRate);
    }

    private void ChoiceA_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentPair == null) return;
        EvaluatePairChoice(CurrentPair.WordA, BtnChoiceA, BtnChoiceB);
    }

    private void ChoiceB_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentPair == null) return;
        EvaluatePairChoice(CurrentPair.WordB, BtnChoiceB, BtnChoiceA);
    }

    private void EvaluatePairChoice(string selectedWord, Button selectedBtn, Button otherBtn)
    {
        if (CurrentPair == null) return;

        bool isCorrect = string.Equals(selectedWord, CurrentPair.TargetWord, StringComparison.OrdinalIgnoreCase);

        if (isCorrect)
        {
            PairFeedbackIcon.Glyph = "\uE73E";
            PairFeedbackIcon.Foreground = (Brush)App.Current.Resources["SystemFillColorSuccessBrush"];
            PairFeedbackText.Text = $"Chính xác! Từ được đọc là '{CurrentPair.TargetWord}' ({CurrentPair.TargetPhonetic}).";
        }
        else
        {
            PairFeedbackIcon.Glyph = "\uE711";
            PairFeedbackIcon.Foreground = (Brush)App.Current.Resources["SystemFillColorCriticalBrush"];
            PairFeedbackText.Text = $"Chưa đúng. Từ thực tế được phát âm là '{CurrentPair.TargetWord}' ({CurrentPair.TargetPhonetic}).";
        }

        PairFeedbackPanel.Visibility = Visibility.Visible;
    }

    private void ResetChoiceButtonsStyle()
    {
        BtnChoiceA.ClearValue(Button.BorderBrushProperty);
        BtnChoiceA.ClearValue(Button.BorderThicknessProperty);
        BtnChoiceB.ClearValue(Button.BorderBrushProperty);
        BtnChoiceB.ClearValue(Button.BorderThicknessProperty);
    }

    private void NextPair_Click(object sender, RoutedEventArgs e)
    {
        _pairIndex++;
        DisplayCurrentPair();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TAB 3: Accent Comparison
    // ─────────────────────────────────────────────────────────────────────────

    private async void PlayUkItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string term && _audioService != null)
        {
            await _audioService.PlayPronunciationAsync(term, "uk", _playbackRate);
        }
    }

    private async void PlayUsItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string term && _audioService != null)
        {
            await _audioService.PlayPronunciationAsync(term, "us", _playbackRate);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
