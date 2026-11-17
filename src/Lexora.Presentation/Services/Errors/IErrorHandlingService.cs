namespace Lexora.Presentation.Services;

/// <summary>
/// Centralized presentation error handling and diagnostic reporting service.
/// </summary>
public interface IErrorHandlingService
{
    /// <summary>
    /// Logs an exception and presents an appropriate user-facing notification or exception dialog.
    /// </summary>
    Task HandleExceptionAsync(
        Exception exception,
        string source,
        string? userMessage = null,
        bool showDialog = true);
}
