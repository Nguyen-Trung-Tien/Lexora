namespace Lexora.Presentation.Services;

/// <summary>
/// Safe, centralized service for opening exported files, revealing files in Explorer, and opening directories.
/// </summary>
public interface IShellLauncherService
{
    void OpenExportedFile(string filePath);
    void RevealInExplorer(string filePath);
    void OpenDirectory(string directoryPath);
}
