using Lexora.Application;
using Lexora.Application.Interfaces;
using Lexora.Infrastructure;
using Lexora.Infrastructure.Data;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Lexora.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Lexora.Presentation;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Window? _window;
    public Window? MainWindow => _window;
    private readonly IHost _host;

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
        };

        InitializeComponent();

        _host = Host.CreateDefaultBuilder()
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateScopes = false;
                options.ValidateOnBuild = false;
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.AddConsole();
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
                services.AddSingleton<IAudioPlayerService, WindowsAudioPlayerService>();
                services.AddSingleton<ISoundFeedbackService, SoundFeedbackService>();
                services.AddSingleton<IToastNotificationService, ToastNotificationService>();
                services.AddSingleton<IStartupOrchestrator, StartupOrchestrator>();

                // ViewModels
                services.AddTransient<ShellViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<VocabularyViewModel>();
                services.AddTransient<ReviewViewModel>();
                services.AddTransient<TestViewModel>();
                services.AddTransient<GrammarViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Views
                services.AddTransient<ShellPage>();
                services.AddTransient<DashboardPage>();
                services.AddTransient<VocabularyPage>();
                services.AddTransient<ReviewPage>();
                services.AddTransient<TestPage>();
                services.AddTransient<GrammarPage>();
                services.AddTransient<SettingsPage>();
            })
            .Build();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // 1. Setup Main Window (hosts the splash overlay and main shell)
        _window = new MainWindow();

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

    private static void LogCrash(string source, Exception? ex, string? extra = null)
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
