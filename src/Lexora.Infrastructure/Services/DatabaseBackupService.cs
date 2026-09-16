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

        // Use SQLite Online Backup API to ensure atomic, WAL-checkpointed snapshot
        using (var sourceConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbFilePath};Mode=ReadOnly"))
        using (var destConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={targetPath}"))
        {
            await sourceConn.OpenAsync(cancellationToken);
            await destConn.OpenAsync(cancellationToken);
            sourceConn.BackupDatabase(destConn);
        }

        return targetPath;
    }

    public async Task RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("Backup file not found.", backupFilePath);

        // 1. Clear all active SQLite connection pools first to release any open file handles
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        // 2. Validate SQLite header magic bytes (16 bytes: "SQLite format 3\0")
        var fileInfo = new FileInfo(backupFilePath);
        if (fileInfo.Length < 16)
        {
            throw new InvalidOperationException("The specified backup file is too small to be a valid SQLite database.");
        }

        byte[] header = new byte[16];
        using (var fs = new FileStream(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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

        // 3. If file is large enough to contain at least 1 page, run SQLite quick_check
        if (fileInfo.Length >= 512)
        {
            try
            {
                using var checkConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={backupFilePath};Mode=ReadOnly");
                await checkConn.OpenAsync(cancellationToken);
                using var checkCmd = checkConn.CreateCommand();
                checkCmd.CommandText = "PRAGMA quick_check(1);";
                var checkResult = await checkCmd.ExecuteScalarAsync(cancellationToken);
                if (checkResult != null && !string.Equals(checkResult.ToString(), "ok", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"The backup file failed SQLite quick integrity check: {checkResult}");
                }
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex)
            {
                throw new InvalidOperationException($"The backup file is corrupt and failed SQLite integrity check: {ex.Message}", ex);
            }
        }

        _logger.LogWarning("Restoring database from {Backup} to {Target}", backupFilePath, _dbFilePath);

        var targetDir = Path.GetDirectoryName(_dbFilePath);
        if (!string.IsNullOrEmpty(targetDir))
            Directory.CreateDirectory(targetDir);

        // 4. Create pre-restore safety snapshot to allow rollback if copy fails
        string? rollbackSnapshot = null;
        if (File.Exists(_dbFilePath))
        {
            rollbackSnapshot = _dbFilePath + ".pre_restore";
            try
            {
                File.Copy(_dbFilePath, rollbackSnapshot, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not create pre-restore snapshot: {Message}", ex.Message);
                rollbackSnapshot = null;
            }
        }

        // 5. Remove stale WAL and SHM files to prevent state mismatch
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

        // 6. Overwrite main database file with rollback safeguard
        try
        {
            using (var sourceStream = new FileStream(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var destStream = new FileStream(_dbFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await sourceStream.CopyToAsync(destStream, cancellationToken);
            }

            // Restore succeeded, cleanup safety snapshot
            if (rollbackSnapshot != null && File.Exists(rollbackSnapshot))
            {
                try { File.Delete(rollbackSnapshot); } catch { /* best effort */ }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore database. Attempting rollback to pre-restore snapshot...");
            if (rollbackSnapshot != null && File.Exists(rollbackSnapshot))
            {
                try
                {
                    File.Copy(rollbackSnapshot, _dbFilePath, true);
                    _logger.LogInformation("Database successfully rolled back to pre-restore snapshot.");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogCritical(rollbackEx, "Failed to rollback database to pre-restore snapshot!");
                }
            }
            throw;
        }
    }
}
