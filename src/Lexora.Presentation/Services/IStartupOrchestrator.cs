namespace Lexora.Presentation.Services;

public enum StartupState
{
    Starting,
    Loading,
    Ready,
    LoadingError,
    Cancelled
}

public interface IStartupOrchestrator
{
    StartupState CurrentState { get; }
    string StatusMessage { get; }
    Exception? LastError { get; }

    event EventHandler<StartupState>? StateChanged;
    event EventHandler<string>? StatusMessageChanged;

    Task InitializeAppAsync(CancellationToken cancellationToken);
    Task RetryAsync(CancellationToken cancellationToken);
}
