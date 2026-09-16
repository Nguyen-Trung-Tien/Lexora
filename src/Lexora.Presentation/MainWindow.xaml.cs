using System.IO;
using System.Runtime.InteropServices;
using Lexora.Infrastructure.Services;
using Lexora.Presentation.Services;
using Lexora.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace Lexora.Presentation;

public sealed partial class MainWindow : Window
{
    private readonly IStartupOrchestrator _startupOrchestrator;
    private readonly ILocalizationService _localizationService;
    private readonly IUserPreferencesService _userPreferencesService;
    private CancellationTokenSource? _startupCts;

    // Min window dimensions (prevents layout clipping on small screens)
    private const int MinWindowWidth = 960;
    private const int MinWindowHeight = 600;

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
        _localizationService = App.Current.Services.GetRequiredService<ILocalizationService>();
        _userPreferencesService = App.Current.Services.GetRequiredService<IUserPreferencesService>();

        // 3. Apply saved window size & position (or center on first run)
        ApplyWindowPlacement();

        UpdateAppTitle();
        _localizationService.LanguageChanged += (s, e) => DispatcherQueue.TryEnqueue(UpdateAppTitle);

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
            var minDisplayTask = Task.Delay(1350, ct);
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

        // Persist window placement for next launch
        SaveWindowPlacement();

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

    private void ApplyWindowPlacement()
    {
        var prefs = _userPreferencesService.GetPreferences();
        var appWindow = AppWindow;

        // Enforce minimum size
        appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
        }

        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest);
        var workArea = displayArea.WorkArea;

        // Clamp desired size to work area, respecting minimum dimensions
        int desiredW = Math.Max(MinWindowWidth, Math.Min(prefs.WindowWidth, workArea.Width));
        int desiredH = Math.Max(MinWindowHeight, Math.Min(prefs.WindowHeight, workArea.Height));

        int posX, posY;
        if (prefs.WindowX < 0 || prefs.WindowY < 0)
        {
            // First run: center window on the primary display work area
            posX = workArea.X + (workArea.Width - desiredW) / 2;
            posY = workArea.Y + (workArea.Height - desiredH) / 2;
        }
        else
        {
            // Restore previous position, but clamp so the window stays on screen
            posX = Math.Max(workArea.X, Math.Min(prefs.WindowX, workArea.X + workArea.Width - desiredW));
            posY = Math.Max(workArea.Y, Math.Min(prefs.WindowY, workArea.Y + workArea.Height - desiredH));
        }

        appWindow.MoveAndResize(new RectInt32(posX, posY, desiredW, desiredH));

        if (prefs.IsMaximized && presenter != null)
        {
            presenter.Maximize();
        }
    }

    private void SaveWindowPlacement()
    {
        try
        {
            var appWindow = AppWindow;
            var presenter = appWindow.Presenter as OverlappedPresenter;
            bool isMaximized = presenter?.State == OverlappedPresenterState.Maximized;

            // Only save the restored (non-maximized) bounds
            var bounds = appWindow.Position;
            var size = appWindow.Size;

            var prefs = _userPreferencesService.GetPreferences() with
            {
                WindowWidth = Math.Max(MinWindowWidth, size.Width),
                WindowHeight = Math.Max(MinWindowHeight, size.Height),
                WindowX = bounds.X,
                WindowY = bounds.Y,
                IsMaximized = isMaximized
            };

            // Fire-and-forget (window is closing, best effort)
            _ = _userPreferencesService.SavePreferencesAsync(prefs);
        }
        catch
        {
            // Graceful fallback on close
        }
    }

    private void UpdateAppTitle()
    {
        var title = _localizationService.GetString("AppTitle");
        Title = title;
        AppTitleTextBlock.Text = title;
    }
}
