namespace Lexora.Application.Interfaces;

/// <summary>
/// Represents a user-facing release notes log entry for a specific application version.
/// </summary>
public record ReleaseNoteItem(
    string Version,
    string Title,
    string ReleaseDate,
    string Summary,
    string MarkdownContent,
    bool IsCurrent = false);

/// <summary>
/// Service contract for retrieving and inspecting offline release notes logs across versions.
/// </summary>
public interface IReleaseNotesService
{
    /// <summary>
    /// Retrieves all available release notes logs ordered by version descending (newest first).
    /// </summary>
    Task<IReadOnlyList<ReleaseNoteItem>> GetAllReleaseNotesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves release notes log for a specific version.
    /// </summary>
    Task<ReleaseNoteItem?> GetReleaseNoteAsync(string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves release notes log for the currently running application version.
    /// </summary>
    Task<ReleaseNoteItem?> GetCurrentVersionReleaseNoteAsync(CancellationToken cancellationToken = default);
}

