using Lexora.Application;
using Lexora.Application.Interfaces;
using Lexora.Infrastructure;
using Lexora.Infrastructure.Data;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Lexora.Presentation.ViewModels.MemoryTips;
using Lexora.Presentation.Views.Common;
using Lexora.Presentation.Views.Dashboard;
using Lexora.Presentation.Views.Grammar;
using Lexora.Presentation.Views.Help;
using Lexora.Presentation.Views.Homophones;
using Lexora.Presentation.Views.KnowledgeGraph;
using Lexora.Presentation.Views.LearningStorage;
using Lexora.Presentation.Views.MemoryTips;
using Lexora.Presentation.Views.Phrases;
using Lexora.Presentation.Views.Review;
using Lexora.Presentation.Views.Roadmap;
using Lexora.Presentation.Views.Settings;
using Lexora.Presentation.Views.Test;
using Lexora.Presentation.Views.Vocabulary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Lexora.Presentation;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Window? _window;
    public Window? MainWindow => _window;
    public Microsoft.UI.Dispatching.DispatcherQueue UIDispatcherQueue { get; }
    private readonly IHost _host;
    private volatile bool _isShuttingDown;
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, IServiceScope> _viewModelScopeTable = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<IServiceScope, byte> _activeScopes = new();

    public static new App Current => (App)Microsoft.UI.Xaml.Application.Current;
    public IServiceProvider Services => _host.Services;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            LogCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception, e.ExceptionObject?.ToString());
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogCrash("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
        UnhandledException += (s, e) =>
        {
            LogCrash("Xaml.UnhandledException", e.Exception, e.Message);
            e.Handled = true;
            if (!_isShuttingDown && e.Exception != null)
            {
                try
                {
                    var errorService = _host?.Services?.GetService<IErrorHandlingService>();
                    if (errorService != null)
                    {
                        _ = errorService.HandleExceptionAsync(e.Exception, "Application.UnhandledException", showDialog: true);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Host is already tearing down during shutdown
                }
            }
        };

        InitializeComponent();

        // Capture the UI-thread DispatcherQueue NOW — App() always runs on the UI thread.
        // The factory lambda below closes over this value so WindowsAudioPlayerService
        // always receives the correct UI DispatcherQueue regardless of which thread
        // resolves the singleton for the first time.
        var uiDispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("App() must be called on the UI thread.");
        UIDispatcherQueue = uiDispatcherQueue;

        _host = Host.CreateDefaultBuilder()
            .UseDefaultServiceProvider(options =>
            {
#if DEBUG
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
#else
                options.ValidateScopes = false;
                options.ValidateOnBuild = false;
#endif
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.AddProvider(new Lexora.Infrastructure.Logging.RollingFileLoggerProvider());
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                // Application and Infrastructure layers
                services.AddApplication();
                services.AddInfrastructure();

                // Presentation Services
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ILocalizationService, LocalizationService>();
                services.AddSingleton<IVoiceProvider, WindowsVoiceProvider>();
                services.AddSingleton<IAudioPlayerService>(sp =>
                    new WindowsAudioPlayerService(
                        sp.GetRequiredService<IAudioStorageService>(),
                        sp.GetRequiredService<IUserPreferencesService>(),
                        sp.GetRequiredService<IPronunciationVoiceResolver>(),
                        uiDispatcherQueue,
                        sp.GetService<ILogger<WindowsAudioPlayerService>>()));
                services.AddSingleton<ISoundFeedbackService, SoundFeedbackService>();
                services.AddSingleton<IToastNotificationService, ToastNotificationService>();
                services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
                services.AddSingleton<IShellLauncherService, ShellLauncherService>();
                services.AddSingleton<IStartupOrchestrator, StartupOrchestrator>();
                services.AddSingleton<IShortcutManager, ShortcutManager>();

                // ViewModels (Isolated IServiceScope per ViewModel to prevent DbContext concurrency collisions across cached pages)
                AddIsolatedScopedViewModel<ShellViewModel>(services);
                AddIsolatedScopedViewModel<DashboardViewModel>(services);
                AddIsolatedScopedViewModel<RoadmapViewModel>(services);
                AddIsolatedScopedViewModel<VocabularyViewModel>(services);
                AddIsolatedScopedViewModel<ReviewViewModel>(services);
                AddIsolatedScopedViewModel<TestViewModel>(services);
                AddIsolatedScopedViewModel<GrammarViewModel>(services);
                AddIsolatedScopedViewModel<HomophonesViewModel>(services);
                AddIsolatedScopedViewModel<PhrasesViewModel>(services);
                AddIsolatedScopedViewModel<MemoryTipsViewModel>(services);
                AddIsolatedScopedViewModel<KnowledgeGraphViewModel>(services);
                AddIsolatedScopedViewModel<LearningStorageViewModel>(services);
                AddIsolatedScopedViewModel<SettingsViewModel>(services);
                AddIsolatedScopedViewModel<HelpViewModel>(services);

                // Views
                services.AddTransient<ShellPage>();
                services.AddTransient<DashboardPage>();
                services.AddTransient<RoadmapPage>();
                services.AddTransient<VocabularyPage>();
                services.AddTransient<HomophonesPage>();
                services.AddTransient<PhrasesPage>();
                services.AddTransient<MemoryTipsPage>();
                services.AddTransient<KnowledgeGraphPage>();
                services.AddTransient<ReviewPage>();
                services.AddTransient<TestPage>();
                services.AddTransient<GrammarPage>();
                services.AddTransient<LearningStoragePage>();
                services.AddTransient<SettingsPage>();
                services.AddTransient<HelpPage>();
            })
            .Build();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // 0. Pre-warm lightweight splash logo asset into memory
        SplashScreenOverlay.PreloadAsset();

        // 1. Setup Main Window (hosts the splash overlay and main shell)
        _window = new MainWindow();
        _window.Closed += (_, _) => DisposeTrackedScopes();

        // 2. Initialize Theme
        var themeService = Services.GetRequiredService<IThemeService>();
        themeService.Initialize(_window);

        // 3. Activate Window immediately with Mica backdrop and Splash Screen
        _window.Activate();

        // 4. Begin Async Startup Orchestration
        if (_window is MainWindow mainWindow)
        {
            _ = mainWindow.StartStartupSequenceAsync();
        }
    }

    private static void AddIsolatedScopedViewModel<TViewModel>(IServiceCollection services)
        where TViewModel : class
    {
        services.AddTransient<TViewModel>(sp =>
        {
            var scope = sp.CreateScope();
            var instance = ActivatorUtilities.CreateInstance<TViewModel>(scope.ServiceProvider);
            _activeScopes.TryAdd(scope, 0);
            _viewModelScopeTable.AddOrUpdate(instance, scope);
            return instance;
        });
    }

    /// <summary>
    /// Returns the isolated IServiceProvider scope associated with a ViewModel, falling back to the root provider.
    /// </summary>
    public static IServiceProvider GetScopeProviderFor(object? viewModel)
    {
        if (viewModel != null && _viewModelScopeTable.TryGetValue(viewModel, out var scope))
        {
            return scope.ServiceProvider;
        }
        return Current.Services;
    }

    /// <summary>
    /// Releases the isolated DI scope (and DbContext) associated with a non-cached Page's ViewModel.
    /// </summary>
    public static void ReleaseScopeForPage(Microsoft.UI.Xaml.Controls.Page? page)
    {
        if (page == null) return;

        var viewModel = page.GetType().GetProperty("ViewModel")?.GetValue(page);
        if (viewModel == null) return;

        if (viewModel is IDisposable disposable)
        {
            try { disposable.Dispose(); } catch { }
        }

        if (_viewModelScopeTable.TryGetValue(viewModel, out var scope))
        {
            _viewModelScopeTable.Remove(viewModel);
            _activeScopes.TryRemove(scope, out _);
            try { scope.Dispose(); } catch { }
        }
    }

    private void DisposeTrackedScopes()
    {
        _isShuttingDown = true;
        foreach (var scope in _activeScopes.Keys.ToList())
        {
            if (_activeScopes.TryRemove(scope, out _))
            {
                try { scope.Dispose(); } catch { }
            }
        }
        try { _host.Dispose(); } catch { }
    }

    public static void LogCrash(string source, Exception? ex, string? extra = null)
    {
        try
        {
            var logDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lexora");
            System.IO.Directory.CreateDirectory(logDir);
            var logPath = System.IO.Path.Combine(logDir, "crash.log");

            var fileInfo = new System.IO.FileInfo(logPath);
            if (fileInfo.Exists && fileInfo.Length > 2 * 1024 * 1024)
            {
                var backupPath = System.IO.Path.Combine(logDir, "crash.old.log");
                if (System.IO.File.Exists(backupPath)) System.IO.File.Delete(backupPath);
                System.IO.File.Move(logPath, backupPath);
            }

            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {extra}\n{ex?.GetType().FullName}: {ex?.Message}\nStackTrace:\n{ex?.StackTrace}\nInnerException: {ex?.InnerException}\n\n";
            System.IO.File.AppendAllText(logPath, text);
        }
        catch { }
    }
}
