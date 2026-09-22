using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Lexora.Presentation.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }
    public Lexora.Presentation.Services.ILocalizationService Loc { get; }
    private readonly Lexora.Presentation.Services.ISoundFeedbackService _soundFeedback;

    public DashboardPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<DashboardViewModel>();
        Loc = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.ILocalizationService>();
        _soundFeedback = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.ISoundFeedbackService>();

        Loc.LanguageChanged += OnLanguageChanged;
        Unloaded += (s, e) => Loc.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
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
        try
        {
            await ViewModel.LoadDashboardAsync();
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
