using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class StudyPlannerDialog : ContentDialog, INotifyPropertyChanged
{
    private readonly IStudyPlannerService _plannerService;
    private readonly INavigationService? _navService;
    public ILocalizationService Loc { get; }

    private DailyCurriculumDto? _curriculum;
    private StudyGoalDto? _goal;

    public DailyCurriculumDto? Curriculum
    {
        get => _curriculum;
        set
        {
            _curriculum = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OverallProgressText));
            OnPropertyChanged(nameof(DaysRemainingText));
            OnPropertyChanged(nameof(PacingBadgeText));
            OnPropertyChanged(nameof(MotivationalTip));
        }
    }

    public string OverallProgressText => Curriculum != null ? $"{Curriculum.OverallProgressPercent:F0}%" : "0%";
    public string DaysRemainingText => Curriculum != null ? string.Format(Loc.GetString("StudyPlanner_DaysRemaining"), Curriculum.DaysRemaining) : string.Empty;
    public string MotivationalTip => Loc.CurrentLanguage.StartsWith("vi")
        ? (Curriculum?.MotivationalTipVi ?? string.Empty)
        : (Curriculum?.MotivationalTipEn ?? string.Empty);

    public string PacingBadgeText => Curriculum?.PacingStatus switch
    {
        "Ahead" => Loc.GetString("StudyPlanner_PacingAhead"),
        "Behind" => Loc.GetString("StudyPlanner_PacingBehind"),
        _ => Loc.GetString("StudyPlanner_PacingOnTrack")
    };

    public StudyPlannerDialog(
        IStudyPlannerService plannerService,
        ILocalizationService loc,
        INavigationService? navService = null)
    {
        _plannerService = plannerService;
        _navService = navService;
        Loc = loc;
        this.InitializeComponent();

        Loaded += async (s, e) =>
        {
            await LoadPlannerDataAsync();
        };
    }

    private async Task LoadPlannerDataAsync()
    {
        _goal = await _plannerService.GetUserGoalAsync();
        Curriculum = await _plannerService.GetTodayCurriculumAsync();

        if (_goal != null)
        {
            TargetScoreBox.Text = _goal.TargetScore;
            TargetDatePicker.Date = new DateTimeOffset(_goal.TargetDate.ToDateTime(TimeOnly.MinValue));
            MinutesSlider.Value = _goal.DailyMinutesGoal;
            NewWordsSlider.Value = _goal.DailyNewWordsGoal;
            ReviewsSlider.Value = _goal.DailyReviewTarget;

            ExamTargetCombo.SelectedIndex = _goal.TargetExam switch
            {
                ExamType.Toeic => 0,
                ExamType.Ielts => 1,
                ExamType.Vstep => 2,
                _ => 3
            };
        }
    }

    private async void SaveGoal_Click(object sender, RoutedEventArgs e)
    {
        var targetDate = TargetDatePicker.Date.HasValue
            ? DateOnly.FromDateTime(TargetDatePicker.Date.Value.DateTime)
            : DateOnly.FromDateTime(DateTime.Today.AddMonths(3));

        var examType = ExamTargetCombo.SelectedIndex switch
        {
            0 => ExamType.Toeic,
            1 => ExamType.Ielts,
            2 => ExamType.Vstep,
            _ => ExamType.General
        };

        var goal = new StudyGoalDto(
            TargetExam: examType,
            TargetScore: string.IsNullOrWhiteSpace(TargetScoreBox.Text) ? "750+" : TargetScoreBox.Text.Trim(),
            TargetDate: targetDate,
            DailyMinutesGoal: (int)MinutesSlider.Value,
            DailyNewWordsGoal: (int)NewWordsSlider.Value,
            DailyReviewTarget: (int)ReviewsSlider.Value,
            IsConfigured: true
        );

        await _plannerService.SaveUserGoalAsync(goal);
        Curriculum = await _plannerService.GetTodayCurriculumAsync();

        GoalSavedInfoBar.IsOpen = true;
    }

    private void TaskNavigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string navTag && _navService != null)
        {
            this.Hide();
            switch (navTag)
            {
                case "Review":
                    _navService.Navigate(typeof(ReviewPage));
                    break;
                case "Vocabulary":
                    _navService.Navigate(typeof(VocabularyPage));
                    break;
                case "Grammar":
                    _navService.Navigate(typeof(GrammarPage));
                    break;
                case "Test":
                    _navService.Navigate(typeof(TestPage));
                    break;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
