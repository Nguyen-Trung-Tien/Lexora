using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Lexora.Presentation.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<DashboardViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadDashboardAsync();
    }

    private void StartReviewButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.StartReviewSession();
    }

    private void StartPracticeButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.StartPracticeTest();
    }
}
