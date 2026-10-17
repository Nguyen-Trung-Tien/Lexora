using Lexora.Application.Interfaces;
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

        Loaded += (s, e) => UpdateResponsiveHero(ActualWidth);
        SizeChanged += (s, e) => UpdateResponsiveHero(e.NewSize.Width);
    }

    private void UpdateResponsiveHero(double width)
    {
        if (HeroGrid == null || HeroActionsPanel == null) return;
        if (width < 1050)
        {
            Grid.SetColumn(HeroActionsPanel, 0);
            Grid.SetRow(HeroActionsPanel, 1);
            HeroActionsPanel.Margin = new Thickness(0, 16, 0, 0);
            HeroActionsPanel.HorizontalAlignment = HorizontalAlignment.Left;
        }
        else
        {
            Grid.SetColumn(HeroActionsPanel, 1);
            Grid.SetRow(HeroActionsPanel, 0);
            HeroActionsPanel.Margin = new Thickness(16, 0, 0, 0);
            HeroActionsPanel.HorizontalAlignment = HorizontalAlignment.Right;
        }
    }

    public void OnLanguageChanged()
    {
        void UpdateUI()
        {
            ViewModel.UpdateLanguageState();
            Bindings.Update();
        }

        if (DispatcherQueue.HasThreadAccess)
        {
            UpdateUI();
        }
        else
        {
            DispatcherQueue.TryEnqueue(UpdateUI);
        }
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

    private async void OpenStudyPlanner_Click(object sender, RoutedEventArgs e)
    {
        _soundFeedback.PlaySoftTapSound();
        try
        {
            var plannerService = App.Current.Services.GetRequiredService<IStudyPlannerService>();
            var navService = App.Current.Services.GetService<INavigationService>();
            var dialog = new StudyPlannerDialog(plannerService, Loc, navService)
            {
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardPage] OpenStudyPlanner error: {ex.Message}");
        }
    }
}
