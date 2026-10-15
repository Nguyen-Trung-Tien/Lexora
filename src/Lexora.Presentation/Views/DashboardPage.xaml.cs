using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Lexora.Presentation.Views;

public sealed partial class DashboardPage : Page, ILocalizableView
{
    public DashboardViewModel ViewModel { get; }
    public ILocalizationService Loc { get; }
    private readonly ISoundFeedbackService _soundFeedback;

    public DashboardPage()
    {
        NavigationCacheMode = NavigationCacheMode.Required;
        ViewModel = App.Current.Services.GetRequiredService<DashboardViewModel>();
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();
        _soundFeedback = App.Current.Services.GetRequiredService<ISoundFeedbackService>();

        InitializeComponent();
    }

    public void OnLanguageChanged()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.UpdateLanguageState();
            Bindings.Update();
        });
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        OnLanguageChanged();
        try
        {
            if (ViewModel.TotalWords == 0)
            {
                await ViewModel.LoadDashboardAsync();
            }
            else
            {
                // Background refresh without freezing UI thread during tab switch
                _ = ViewModel.LoadDashboardAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardPage] OnNavigatedTo error: {ex.Message}");
        }
    }

    private void StartReviewButton_Click(object sender, RoutedEventArgs e)
    {
        _soundFeedback.PlaySoftTapSound();
        ViewModel.StartReviewSession();
    }

    private void StartPracticeButton_Click(object sender, RoutedEventArgs e)
    {
        _soundFeedback.PlaySoftTapSound();
        ViewModel.StartPracticeTest();
    }

    private void TotalWordsCard_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        _soundFeedback.PlaySoftTapSound();
        ViewModel.OpenVocabulary();
    }

    private void DueTodayCard_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        _soundFeedback.PlaySoftTapSound();
        ViewModel.StartReviewSession();
    }

    private void StreakCard_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        _soundFeedback.PlayStreakSound();
    }
}
