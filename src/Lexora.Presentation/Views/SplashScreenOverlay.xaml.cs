using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using Lexora.Presentation.Services;

namespace Lexora.Presentation.Views;

public sealed partial class SplashScreenOverlay : UserControl
{
    private readonly UISettings _uiSettings = new();
    private TaskCompletionSource<bool>? _entranceTcs;
    private TaskCompletionSource<bool>? _exitTcs;

    public event EventHandler? RetryRequested;
    public event EventHandler? ExitRequested;

    public bool IsAnimationEnabled => _uiSettings.AnimationsEnabled;

    public SplashScreenOverlay()
    {
        InitializeComponent();
        UpdateLocalizedText();

        EntranceStoryboard.Completed += (s, e) => _entranceTcs?.TrySetResult(true);
        ExitStoryboard.Completed += (s, e) =>
        {
            Visibility = Visibility.Collapsed;
            _exitTcs?.TrySetResult(true);
        };
        ReducedMotionEntrance.Completed += (s, e) => _entranceTcs?.TrySetResult(true);
    }

    public void UpdateLocalizedText()
    {
        TaglineTextBlock.Text = LocalizationHelper.GetString("Splash_Tagline");
        StatusTextBlock.Text = LocalizationHelper.GetString("Splash_StatusStarting");
        ErrorTitleTextBlock.Text = LocalizationHelper.GetString("Splash_InitFailed");
        ErrorDetailsTextBlock.Text = LocalizationHelper.GetString("Splash_InitFailedDesc");
        RetryButton.Content = LocalizationHelper.GetString("Splash_Retry");
        ExitButton.Content = LocalizationHelper.GetString("Splash_Exit");
    }

    public async Task PlayEntranceAsync()
    {
        _entranceTcs = new TaskCompletionSource<bool>();

        if (!IsAnimationEnabled)
        {
            ReducedMotionEntrance.Begin();
        }
        else
        {
            EntranceStoryboard.Begin();
        }

        await _entranceTcs.Task;
    }

    public async Task PlayExitAsync()
    {
        _exitTcs = new TaskCompletionSource<bool>();

        if (!IsAnimationEnabled)
        {
            RootGrid.Opacity = 0;
            Visibility = Visibility.Collapsed;
            _exitTcs.TrySetResult(true);
        }
        else
        {
            ExitStoryboard.Begin();
        }

        await _exitTcs.Task;
    }

    public void UpdateStatus(string message)
    {
        StatusTextBlock.Text = message;
    }

    public void ShowLoadingState()
    {
        LoadingContainer.Visibility = Visibility.Visible;
        ErrorCard.Visibility = Visibility.Collapsed;
    }

    public void ShowError(string errorMessage)
    {
        LoadingContainer.Visibility = Visibility.Collapsed;
        ErrorDetailsTextBlock.Text = string.IsNullOrWhiteSpace(errorMessage)
            ? LocalizationHelper.GetString("Splash_InitFailedDesc")
            : errorMessage;
        ErrorCard.Visibility = Visibility.Visible;
    }

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        ShowLoadingState();
        RetryRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }
}
