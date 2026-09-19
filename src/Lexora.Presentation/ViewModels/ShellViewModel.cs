using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _localizationService;
    private readonly IReviewService _reviewService;
    private readonly Lexora.Infrastructure.Services.IUserPreferencesService _preferencesService;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReminderVisible))]
    public partial bool HasDueReviews { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReminderVisible))]
    public partial bool IsOnReviewPage { get; set; } = false;

    public bool IsReminderVisible => HasDueReviews && !IsOnReviewPage;

    [ObservableProperty]
    public partial string DueReviewMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReminderTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReminderActionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int DueCardsCount { get; set; } = 0;

    public ShellViewModel(
        INavigationService navigationService,
        ILocalizationService localizationService,
        IReviewService reviewService,
        Lexora.Infrastructure.Services.IUserPreferencesService preferencesService)
    {
        _navigationService = navigationService;
        _localizationService = localizationService;
        _reviewService = reviewService;
        _preferencesService = preferencesService;

        UpdateLocalizedTexts();
        _localizationService.LanguageChanged += async (s, e) =>
        {
            UpdateLocalizedTexts();
            await CheckDueReviewsAsync();
        };
        _preferencesService.PreferencesChanged += async (s, prefs) => await CheckDueReviewsAsync();
    }

    public async Task CheckDueReviewsAsync()
    {
        try
        {
            var prefs = _preferencesService.GetPreferences();
            if (!prefs.EnableReviewReminder)
            {
                HasDueReviews = false;
                DueReviewMessage = string.Empty;
                return;
            }

            int count = await _reviewService.GetDueCardsCountAsync();
            DueCardsCount = count;
            HasDueReviews = count > 0;
            var template = _localizationService.GetString("Review_Reminder_Msg");
            DueReviewMessage = count > 0 
                ? (template.Contains("{0}") ? string.Format(template, count) : $"{template} {count}") 
                : string.Empty;
        }
        catch
        {
            // Ignore background check failure
        }
    }

    public void UpdateLocalizedTexts()
    {
        Title = _localizationService.GetString("AppTitle");
        ReminderTitle = _localizationService.GetString("Review_Reminder_Title");
        ReminderActionText = _localizationService.GetString("Review_Now");
        if (HasDueReviews && DueCardsCount > 0)
        {
            var template = _localizationService.GetString("Review_Reminder_Msg");
            DueReviewMessage = template.Contains("{0}") ? string.Format(template, DueCardsCount) : $"{template} {DueCardsCount}";
        }
    }

    [RelayCommand]
    private void Navigate(Type pageType)
    {
        _navigationService.Navigate(pageType);
    }
}
