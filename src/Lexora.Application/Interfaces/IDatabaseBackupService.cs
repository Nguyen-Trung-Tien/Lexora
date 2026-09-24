using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IDatabaseBackupService
{
    string DatabasePath { get; }
    Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default);
    Task<string> CreateBackupToFileAsync(string targetFilePath, CancellationToken cancellationToken = default);
    Task<BackupMetadata> GetBackupMetadataAsync(string backupFilePath, CancellationToken cancellationToken = default);
    Task RestoreBackupAsync(string backupFilePath, IProgress<(int Percent, string StepMessage, string? Detail)>? progress = null, CancellationToken cancellationToken = default);
}
