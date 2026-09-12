using System.IO;
using System.Runtime.InteropServices;
using Lexora.Presentation.Services;
using Lexora.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Lexora.Presentation;

public sealed partial class MainWindow : Window
{
    private readonly IStartupOrchestrator _startupOrchestrator;
    private CancellationTokenSource? _startupCts;

    public MainWindow()
    {
        InitializeComponent();

        // 1. Extend content into TitleBar & set custom title bar
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // 2. Set Taskbar and Window Icon
        SetWindowAndTaskbarIcon();

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

    private void SetWindowAndTaskbarIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(iconPath))
            {
                // Set Windows App SDK window icon
                AppWindow.SetIcon(iconPath);

                // Set Win32 window & taskbar icon via SendMessage WM_SETICON
                var hwnd = WindowNative.GetWindowHandle(this);
                if (hwnd != IntPtr.Zero)
                {
                    IntPtr hIconSmall = LoadImage(IntPtr.Zero, iconPath, 1 /*IMAGE_ICON*/, 16, 16, 0x00000010 /*LR_LOADFROMFILE*/);
                    IntPtr hIconBig = LoadImage(IntPtr.Zero, iconPath, 1 /*IMAGE_ICON*/, 32, 32, 0x00000010 /*LR_LOADFROMFILE*/);

                    if (hIconSmall != IntPtr.Zero)
                    {
                        SendMessage(hwnd, 0x0080 /*WM_SETICON*/, (IntPtr)0 /*ICON_SMALL*/, hIconSmall);
                    }
                    if (hIconBig != IntPtr.Zero)
                    {
                        SendMessage(hwnd, 0x0080 /*WM_SETICON*/, (IntPtr)1 /*ICON_BIG*/, hIconBig);
                    }
                }
            }
        }
        catch
        {
            // Fallback gracefully
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

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
            var minDisplayTask = Task.Delay(1100, ct);
            var initTask = _startupOrchestrator.InitializeAppAsync(ct);

            // Wait for backend initialization
            await initTask;

            if (_startupOrchestrator.CurrentState == StartupState.Ready)
            {
                // Ensure entrance animation and minimal display timing finish smoothly
                await Task.WhenAll(entranceTask, minDisplayTask);

                // Pre-navigate to ShellPage while splash is still covering
                RootFrame.Navigate(typeof(ShellPage));

                // Seamless concurrent cross-fade: reveal main shell while fading out splash
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

                await SplashOverlay.PlayExitAsync();
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
