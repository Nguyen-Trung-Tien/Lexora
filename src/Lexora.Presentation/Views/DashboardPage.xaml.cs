using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Lexora.Presentation.Views;

public sealed partial class DashboardPage : Page, ILocalizableView, IThemeAwareView
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
        ActualThemeChanged += (s, e) => OnThemeChanged();
    }

    private void UpdateResponsiveHero(double width)
    {
        if (HeroGrid == null || HeroActionsPanel == null) return;
        if (width < 1250)
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

        if (BreakdownGrid != null && CefrBreakdownBorder != null && PosBreakdownBorder != null)
        {
            if (width < 850)
            {
                if (BreakdownGrid.ColumnDefinitions.Count > 1)
                {
                    BreakdownGrid.ColumnDefinitions.Clear();
                    BreakdownGrid.RowDefinitions.Clear();
                    BreakdownGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    BreakdownGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    BreakdownGrid.RowSpacing = 16;

                    Grid.SetColumn(CefrBreakdownBorder, 0);
                    Grid.SetRow(CefrBreakdownBorder, 0);

                    Grid.SetColumn(PosBreakdownBorder, 0);
                    Grid.SetRow(PosBreakdownBorder, 1);
                }
            }
            else
            {
                if (BreakdownGrid.ColumnDefinitions.Count <= 1)
                {
                    BreakdownGrid.RowDefinitions.Clear();
                    BreakdownGrid.ColumnDefinitions.Clear();
                    BreakdownGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    BreakdownGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    BreakdownGrid.RowSpacing = 0;

                    Grid.SetColumn(CefrBreakdownBorder, 0);
                    Grid.SetRow(CefrBreakdownBorder, 0);

                    Grid.SetColumn(PosBreakdownBorder, 1);
                    Grid.SetRow(PosBreakdownBorder, 0);
                }
            }
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

    public void OnThemeChanged()
    {
        void UpdateThemeUI()
        {
            ViewModel.RefreshVisuals();
            Bindings.Update();
        }

        if (DispatcherQueue.HasThreadAccess)
        {
            UpdateThemeUI();
        }
        else
        {
            DispatcherQueue.TryEnqueue(UpdateThemeUI);
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        OnLanguageChanged();
        OnThemeChanged();
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
            await DispatcherQueue.EnsureOnUIThreadAsync();
            var xamlRoot = App.Current.MainWindow?.Content?.XamlRoot ?? this.XamlRoot;
            var plannerService = App.Current.Services.GetRequiredService<IStudyPlannerService>();
            var navService = App.Current.Services.GetService<INavigationService>();
            var dialog = new StudyPlannerDialog(plannerService, Loc, navService)
            {
                XamlRoot = xamlRoot
            };
            await dialog.ShowAsyncSafe(xamlRoot);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardPage] OpenStudyPlanner error: {ex.Message}");
        }
    }

    private async void OpenListeningLab_Click(object sender, RoutedEventArgs e)
    {
        _soundFeedback.PlaySoftTapSound();
        try
        {
            await DispatcherQueue.EnsureOnUIThreadAsync();
            var xamlRoot = App.Current.MainWindow?.Content?.XamlRoot ?? this.XamlRoot;
            var labService = App.Current.Services.GetRequiredService<IPronunciationListeningLabService>();
            var audioService = App.Current.Services.GetService<IAudioPlayerService>();
            var dialog = new ListeningLabDialog(labService, Loc, audioService)
            {
                XamlRoot = xamlRoot
            };
            await dialog.ShowAsyncSafe(xamlRoot);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardPage] OpenListeningLab error: {ex.Message}");
        }
    }
}
