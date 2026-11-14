namespace Lexora.Application.DTOs;

/// <summary>
/// Represents a structured progress notification for long-running operations
/// such as vocabulary imports/exports, database backups, and audio downloads.
/// </summary>
/// <param name="Percent">Progress percentage from 0 to 100.</param>
/// <param name="Current">The current item/record being processed.</param>
/// <param name="Total">The total number of items/records to process.</param>
/// <param name="Message">User-facing progress status message.</param>
/// <param name="StepDetail">Optional granular step details (e.g. current term being validated).</param>
public record ProgressReport(
    int Percent,
    int Current,
    int Total,
    string Message,
    string? StepDetail = null);
