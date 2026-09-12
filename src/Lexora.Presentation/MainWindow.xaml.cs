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

                // Set Win32 window & taskbar icon via SendMessage WM_SETICON and SetClassLongPtr
                var hwnd = WindowNative.GetWindowHandle(this);
                if (hwnd != IntPtr.Zero)
                {
                    var cxSmall = Math.Max(GetSystemMetrics(49 /*SM_CXSMICON*/), 16);
                    var cySmall = Math.Max(GetSystemMetrics(50 /*SM_CYSMICON*/), 16);
                    var cxBig = Math.Max(GetSystemMetrics(11 /*SM_CXICON*/), 48);
                    var cyBig = Math.Max(GetSystemMetrics(12 /*SM_CYICON*/), 48);

                    _hIconSmall = LoadImage(IntPtr.Zero, iconPath, 1 /*IMAGE_ICON*/, cxSmall, cySmall, 0x00000010 /*LR_LOADFROMFILE*/);
                    _hIconBig = LoadImage(IntPtr.Zero, iconPath, 1 /*IMAGE_ICON*/, cxBig, cyBig, 0x00000010 /*LR_LOADFROMFILE*/);

                    if (_hIconSmall != IntPtr.Zero)
                    {
                        SendMessage(hwnd, 0x0080 /*WM_SETICON*/, (IntPtr)0 /*ICON_SMALL*/, _hIconSmall);
                        SetClassLongPtr(hwnd, -34 /*GCLP_HICONSM*/, _hIconSmall);
                    }
                    if (_hIconBig != IntPtr.Zero)
                    {
                        SendMessage(hwnd, 0x0080 /*WM_SETICON*/, (IntPtr)1 /*ICON_BIG*/, _hIconBig);
                        SetClassLongPtr(hwnd, -14 /*GCLP_HICON*/, _hIconBig);
                    }
                }
            }
        }
        catch
        {
            // Fallback gracefully
        }
    }

    private IntPtr _hIconSmall = IntPtr.Zero;
    private IntPtr _hIconBig = IntPtr.Zero;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetClassLongPtr")]
    private static extern IntPtr SetClassLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetClassLong")]
    private static extern IntPtr SetClassLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8 ? SetClassLongPtr64(hWnd, nIndex, dwNewLong) : SetClassLong32(hWnd, nIndex, dwNewLong);
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

        if (_hIconSmall != IntPtr.Zero)
        {
            DestroyIcon(_hIconSmall);
            _hIconSmall = IntPtr.Zero;
        }

        if (_hIconBig != IntPtr.Zero)
        {
            DestroyIcon(_hIconBig);
            _hIconBig = IntPtr.Zero;
        }
    }
}
