using System.Diagnostics;

namespace Lexora.Presentation.Services;

public sealed class ShellLauncherService : IShellLauncherService
{
    private static readonly HashSet<string> AllowedExportExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".csv", ".json", ".txt", ".db", ".tsv"
    };

    public void OpenExportedFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

        try
        {
            var cleanPath = Path.GetFullPath(filePath);
            var ext = Path.GetExtension(cleanPath);
            if (!AllowedExportExtensions.Contains(ext)) return;

            Process.Start(new ProcessStartInfo(cleanPath) { UseShellExecute = true });
        }
        catch
        {
            // Ignore shell launch failures
        }
    }

    public void RevealInExplorer(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

        try
        {
            var cleanPath = Path.GetFullPath(filePath);
            var psi = new ProcessStartInfo("explorer.exe")
            {
                UseShellExecute = false
            };
            psi.ArgumentList.Add($"/select,{cleanPath}");
            Process.Start(psi);
        }
        catch
        {
            // Ignore Explorer launch failures
        }
    }

    public void OpenDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath)) return;

        try
        {
            var cleanPath = Path.GetFullPath(directoryPath);
            Directory.CreateDirectory(cleanPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = cleanPath,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore directory launch failures
        }
    }
}
