using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.Services;

public interface IDatabaseBackupService
{
    string DatabasePath { get; }
    Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default);
    Task RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);
}

public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly string _dbFilePath;
    private readonly ILogger<DatabaseBackupService> _logger;

    public string DatabasePath => _dbFilePath;

    public DatabaseBackupService(string dbFilePath, ILogger<DatabaseBackupService> logger)
    {
        _dbFilePath = dbFilePath;
        _logger = logger;
    }

    public async Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_dbFilePath))
            throw new FileNotFoundException("Database file not found.", _dbFilePath);

        Directory.CreateDirectory(destinationDirectory);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"lexora_backup_{timestamp}.db";
        var targetPath = Path.Combine(destinationDirectory, backupFileName);

        _logger.LogInformation("Creating backup from {Source} to {Target}", _dbFilePath, targetPath);

        // Copy file
        using (var sourceStream = new FileStream(_dbFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var destStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(destStream, cancellationToken);
        }

        return targetPath;
    }

    public async Task RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("Backup file not found.", backupFilePath);

        _logger.LogWarning("Restoring database from {Backup} to {Target}", backupFilePath, _dbFilePath);

        var targetDir = Path.GetDirectoryName(_dbFilePath);
        if (!string.IsNullOrEmpty(targetDir))
            Directory.CreateDirectory(targetDir);

        using (var sourceStream = new FileStream(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var destStream = new FileStream(_dbFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(destStream, cancellationToken);
        }
    }
}
