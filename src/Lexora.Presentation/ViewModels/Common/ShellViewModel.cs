using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.ViewModels;

public partial class ShellViewModel : ObservableObject, IDisposable
{
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _localizationService;
    private readonly IReviewService _reviewService;
    private readonly IUserPreferencesService _preferencesService;
    private readonly IUpdateService _updateService;
    private readonly IToastNotificationService _toastService;
    private readonly EventHandler? _languageChangedHandler;
    private readonly EventHandler<Lexora.Application.DTOs.UserPreferences>? _prefsChangedHandler;
    private bool _hasShownDueReviewToast = false;

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

    [ObservableProperty]
    public partial bool HasUpdateAvailable { get; set; } = false;

    [ObservableProperty]
    public partial Lexora.Application.DTOs.UpdateInfo? AvailableUpdate { get; set; }

    public ShellViewModel(
        INavigationService navigationService,
        ILocalizationService localizationService,
        IReviewService reviewService,
        IUserPreferencesService preferencesService,
        IUpdateService updateService,
        IToastNotificationService toastService)
    {
        _navigationService = navigationService;
        _localizationService = localizationService;
        _reviewService = reviewService;
        _preferencesService = preferencesService;
        _updateService = updateService;
        _toastService = toastService;

        UpdateLocalizedTexts();
        _languageChangedHandler = async (s, e) =>
        {
            UpdateLocalizedTexts();
            await CheckDueReviewsAsync();
        };
        _prefsChangedHandler = async (s, prefs) => await CheckDueReviewsAsync();

        _localizationService.LanguageChanged += _languageChangedHandler;
        _preferencesService.PreferencesChanged += _prefsChangedHandler;
    }

    public void Dispose()
    {
        if (_languageChangedHandler != null)
        {
            _localizationService.LanguageChanged -= _languageChangedHandler;
        }
        if (_prefsChangedHandler != null)
        {
            _preferencesService.PreferencesChanged -= _prefsChangedHandler;
        }
        GC.SuppressFinalize(this);
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

            if (count > 0 && !_hasShownDueReviewToast)
            {
                _hasShownDueReviewToast = true;
                var title = _localizationService.GetString("Review_Reminder_Title");
                var actionText = _localizationService.GetString("Review_Now");
                _toastService.ShowInfo(
                    DueReviewMessage,
                    title,
                    durationMs: 7000,
                    actionText: actionText,
                    actionCallback: () =>
                    {
                        _navigationService.Navigate(typeof(Views.ReviewPage));
                    });
            }
        }
        catch
        {
            // Ignore background check failure
        }
    }

    public async Task CheckForUpdatesAsync()
    {
        try
        {
            var prefs = _preferencesService.GetPreferences();
            if (!prefs.AutoCheckUpdates)
            {
                return;
            }

            var update = await _updateService.CheckForUpdatesAsync(isManualCheck: false);
            if (update != null && update.HasUpdate)
            {
                AvailableUpdate = update;
                HasUpdateAvailable = true;

                var title = _localizationService.GetString("Update_Notification_Title");
                var template = _localizationService.GetString("Update_Notification_Body");
                var msg = string.Format(template, update.NewVersion);
                var actionText = _localizationService.GetString("Update_Notification_Action");

                _toastService.ShowInfo(
                    msg,
                    title,
                    durationMs: 7000,
                    actionText: actionText,
                    actionCallback: () =>
                    {
                        _navigationService.Navigate(typeof(Views.SettingsPage));
                    });
            }
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
