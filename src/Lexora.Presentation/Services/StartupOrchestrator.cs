using Lexora.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexora.Presentation.Services;

public class StartupOrchestrator : IStartupOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupOrchestrator> _logger;

    private StartupState _currentState = StartupState.Starting;
    private string _statusMessage = string.Empty;
    private double _currentProgress = 0.0;
    private Exception? _lastError;

    public StartupState CurrentState => _currentState;
    public string StatusMessage => _statusMessage;
    public double CurrentProgress => _currentProgress;
    public Exception? LastError => _lastError;

    public event EventHandler<StartupState>? StateChanged;
    public event EventHandler<string>? StatusMessageChanged;
    public event EventHandler<double>? ProgressChanged;

    public StartupOrchestrator(
        IServiceProvider serviceProvider,
        ILogger<StartupOrchestrator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        var loc = _serviceProvider.GetService<ILocalizationService>();
        _statusMessage = loc?.GetString("Splash_StatusStarting") ?? "Initializing...";
        _currentProgress = 0.12;
    }

    public async Task InitializeAppAsync(CancellationToken cancellationToken)
    {
        var loc = _serviceProvider.GetService<ILocalizationService>();
        _lastError = null;
        SetState(StartupState.Starting);
        SetProgress(0.12);
        SetStatus(loc?.GetString("Splash_StatusBooting") ?? "Starting Lexora core systems...");

        try
        {
            // Step 1: Services setup & starting state
            cancellationToken.ThrowIfCancellationRequested();
            SetState(StartupState.Loading);
            SetProgress(0.35);
            SetStatus(loc?.GetString("Splash_StatusCheckingDb") ?? "Checking SQLite database & schema...");

            // Step 2: Initialize SQLite Database & seed data
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<SqliteDatabaseInitializer>();
                await dbInitializer.InitializeAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            SetProgress(0.70);

            // Step 3: Load Settings & Environment
            SetStatus(loc?.GetString("Splash_StatusLoadingData") ?? "Loading study data & preferences...");

            // Step 4: Ready
            cancellationToken.ThrowIfCancellationRequested();
            SetProgress(1.00);
            SetStatus(loc?.GetString("Splash_StatusReady") ?? "Ready to launch!");
            SetState(StartupState.Ready);
            _logger.LogInformation("Lexora application initialization completed successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Lexora startup sequence was cancelled.");
            SetState(StartupState.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lexora startup sequence failed.");
            _lastError = ex;
            var errFormat = loc?.GetString("Splash_StatusErrorFormat") ?? "Initialization error: {0}";
            SetStatus(string.Format(errFormat, ex.Message));
            SetState(StartupState.LoadingError);
        }
    }

    public async Task RetryAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrying Lexora application initialization...");
        await InitializeAppAsync(cancellationToken);
    }

    private void SetState(StartupState state)
    {
        _currentState = state;
        StateChanged?.Invoke(this, state);
    }

    private void SetStatus(string message)
    {
        _statusMessage = message;
        StatusMessageChanged?.Invoke(this, message);
    }

    private void SetProgress(double progress)
    {
        _currentProgress = Math.Clamp(progress, 0.0, 1.0);
        ProgressChanged?.Invoke(this, _currentProgress);
    }
}
