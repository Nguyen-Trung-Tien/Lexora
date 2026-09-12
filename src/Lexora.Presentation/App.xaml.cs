using Lexora.Application;
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
        InitializeComponent();

        _host = Host.CreateDefaultBuilder()
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

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // 1. Initialize SQLite Database & seed data
        using (var scope = Services.CreateScope())
        {
            var dbInitializer = scope.ServiceProvider.GetRequiredService<SqliteDatabaseInitializer>();
            await dbInitializer.InitializeAsync();
        }

        // 2. Setup Main Window
        _window = new MainWindow();

        // 3. Initialize Theme
        var themeService = Services.GetRequiredService<IThemeService>();
        themeService.Initialize(_window);

        // 4. Activate Window
        _window.Activate();
    }
}
