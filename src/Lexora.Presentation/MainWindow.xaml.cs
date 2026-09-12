using Lexora.Presentation.Services;
using Lexora.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace Lexora.Presentation;

public sealed partial class MainWindow : Window
{
    private readonly IStartupOrchestrator _startupOrchestrator;
    private CancellationTokenSource? _startupCts;

    public MainWindow()
    {
        InitializeComponent();

        _startupOrchestrator = App.Current.Services.GetRequiredService<IStartupOrchestrator>();

        // Wire up cancellation on window close
        Closed += OnWindowClosed;

        // Wire up Splash screen actions
        SplashOverlay.RetryRequested += async (s, e) => await StartStartupSequenceAsync();
        SplashOverlay.ExitRequested += (s, e) => Close();

        _startupOrchestrator.StatusMessageChanged += (s, msg) =>
        {
            DispatcherQueue.TryEnqueue(() => SplashOverlay.UpdateStatus(msg));
        };

        _startupOrchestrator.StateChanged += (s, state) =>
        {
            if (state == StartupState.LoadingError)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    var errorMsg = _startupOrchestrator.LastError?.Message
                        ?? "Không thể khởi tạo cơ sở dữ liệu SQLite hoặc cấu hình ứng dụng.";
                    SplashOverlay.ShowError(errorMsg);
                });
            }
        };
    }

    public async Task StartStartupSequenceAsync()
    {
        _startupCts?.Cancel();
        _startupCts?.Dispose();
        _startupCts = new CancellationTokenSource();
        var ct = _startupCts.Token;

        try
        {
            SplashOverlay.ShowLoadingState();

            // Run entrance animation and backend initialization concurrently
            var entranceTask = SplashOverlay.PlayEntranceAsync();
            var minDisplayTask = Task.Delay(1800, ct);
            var initTask = _startupOrchestrator.InitializeAppAsync(ct);

            // Wait for backend initialization
            await initTask;

            if (_startupOrchestrator.CurrentState == StartupState.Ready)
            {
                // Ensure entrance animation and minimal display timing finish smoothly
                await Task.WhenAll(entranceTask, minDisplayTask);

                // Pre-navigate to ShellPage while splash is still covering
                RootFrame.Navigate(typeof(ShellPage));

                // Smoothly fade out splash and reveal main application shell
                await SplashOverlay.PlayExitAsync();

                if (SplashOverlay.IsAnimationEnabled)
                {
                    MainEntranceStoryboard.Begin();
                }
                else
                {
                    MainContentGrid.Opacity = 1.0;
                    MainContentScale.ScaleX = 1.0;
                    MainContentScale.ScaleY = 1.0;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // App was closed or cancelled
        }
        catch (Exception ex)
        {
            SplashOverlay.ShowError(ex.Message);
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _startupCts?.Cancel();
        _startupCts?.Dispose();
        _startupCts = null;
    }
}
