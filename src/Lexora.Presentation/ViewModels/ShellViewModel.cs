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

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasDueReviews { get; set; } = false;

    [ObservableProperty]
    public partial string DueReviewMessage { get; set; } = string.Empty;

    public ShellViewModel(INavigationService navigationService, ILocalizationService localizationService, IReviewService reviewService)
    {
        _navigationService = navigationService;
        _localizationService = localizationService;
        _reviewService = reviewService;

        UpdateTitle();
        _localizationService.LanguageChanged += (s, e) => UpdateTitle();
    }

    public async Task CheckDueReviewsAsync()
    {
        try
        {
            int count = await _reviewService.GetDueCardsCountAsync();
            HasDueReviews = count > 0;
            DueReviewMessage = count > 0 ? $"You have {count} word{(count > 1 ? "s" : "")} ready for spaced repetition review." : string.Empty;
        }
        catch
        {
            // Ignore background check failure
        }
    }

    private void UpdateTitle()
    {
        Title = _localizationService.GetString("AppTitle");
    }

    [RelayCommand]
    private void Navigate(Type pageType)
    {
        _navigationService.Navigate(pageType);
    }
}
