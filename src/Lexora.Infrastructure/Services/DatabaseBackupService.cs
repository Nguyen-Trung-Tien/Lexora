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

        // 1. Validate SQLite header magic bytes (16 bytes: "SQLite format 3\0")
        var fileInfo = new FileInfo(backupFilePath);
        if (fileInfo.Length < 16)
        {
            throw new InvalidOperationException("The specified backup file is too small to be a valid SQLite database.");
        }

        byte[] header = new byte[16];
        using (var fs = new FileStream(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            int read = await fs.ReadAsync(header.AsMemory(0, 16), cancellationToken);
            if (read < 16)
            {
                throw new InvalidOperationException("Failed to read header from the backup file.");
            }
        }

        var headerString = System.Text.Encoding.ASCII.GetString(header, 0, 15);
        if (!string.Equals(headerString, "SQLite format 3", StringComparison.Ordinal) || header[15] != 0)
        {
            throw new InvalidOperationException("The specified file is not a valid SQLite database (invalid header).");
        }

        _logger.LogWarning("Restoring database from {Backup} to {Target}", backupFilePath, _dbFilePath);

        // 2. Clear all active SQLite connection pools to prevent file locking and corruption
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        var targetDir = Path.GetDirectoryName(_dbFilePath);
        if (!string.IsNullOrEmpty(targetDir))
            Directory.CreateDirectory(targetDir);

        // 3. Remove stale WAL and SHM files to prevent state mismatch
        var walFile = _dbFilePath + "-wal";
        var shmFile = _dbFilePath + "-shm";
        if (File.Exists(walFile))
        {
            try { File.Delete(walFile); } catch { /* best effort */ }
        }
        if (File.Exists(shmFile))
        {
            try { File.Delete(shmFile); } catch { /* best effort */ }
        }

        // 4. Overwrite main database file
        using (var sourceStream = new FileStream(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var destStream = new FileStream(_dbFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(destStream, cancellationToken);
        }
    }
}
