using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IDatabaseBackupService
{
    string DatabasePath { get; }
    Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default);
    Task<string> CreateBackupToFileAsync(string targetFilePath, CancellationToken cancellationToken = default);
    Task<BackupMetadata> GetBackupMetadataAsync(string backupFilePath, CancellationToken cancellationToken = default);
    Task RestoreBackupAsync(string backupFilePath, IProgress<(int Percent, string StepMessage, string? Detail)>? progress = null, CancellationToken cancellationToken = default);
    Task<bool> ValidateDatabaseIntegrityAsync(string? filePath = null, CancellationToken cancellationToken = default);
    Task<string?> EnsureAutoBackupAsync(int maxRetainedBackups = 5, TimeSpan? minInterval = null, bool force = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> RotateBackupsAsync(string directory, int maxRetainedBackups = 5, string pattern = "lexora_autobackup_*.db", CancellationToken cancellationToken = default);
}
