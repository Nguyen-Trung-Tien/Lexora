using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }
    private readonly INavigationService _navService;

    public DashboardPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<DashboardViewModel>();
        _navService = App.Current.Services.GetRequiredService<INavigationService>();

        Loaded += async (s, e) =>
        {
            await ViewModel.LoadDashboardAsync();
        };
    }

    private void StartReviewButton_Click(object sender, RoutedEventArgs e)
    {
        _navService.Navigate(typeof(ReviewPage));
    }
}
