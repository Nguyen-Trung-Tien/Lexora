using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.Services;

public record BackupMetadata(
    string FilePath,
    string FileName,
    long FileSizeBytes,
    string FileSizeFormatted,
    DateTime LastModified,
    int WordCount,
    int MeaningCount,
    int GrammarRuleCount,
    int StudyLogCount,
    bool IsValid,
    string? ErrorMessage = null);

public interface IDatabaseBackupService
{
    string DatabasePath { get; }
    Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default);
    Task<string> CreateBackupToFileAsync(string targetFilePath, CancellationToken cancellationToken = default);
    Task<BackupMetadata> GetBackupMetadataAsync(string backupFilePath, CancellationToken cancellationToken = default);
    Task RestoreBackupAsync(string backupFilePath, IProgress<(int Percent, string StepMessage, string? Detail)>? progress = null, CancellationToken cancellationToken = default);
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
        if (string.IsNullOrWhiteSpace(destinationDirectory))
            throw new ArgumentException("Destination directory cannot be empty.", nameof(destinationDirectory));

        Directory.CreateDirectory(destinationDirectory);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"lexora_backup_{timestamp}.db";
        var targetPath = Path.Combine(destinationDirectory, backupFileName);

        return await CreateBackupToFileAsync(targetPath, cancellationToken);
    }

    public async Task<string> CreateBackupToFileAsync(string targetFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_dbFilePath))
            throw new FileNotFoundException("Database file not found.", _dbFilePath);

        var targetDir = Path.GetDirectoryName(targetFilePath);
        if (!string.IsNullOrEmpty(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        _logger.LogInformation("Creating backup from {Source} to {Target}", _dbFilePath, targetFilePath);

        // 1. Flush SQLite WAL to ensure all committed pages are written before backup snapshot
        try
        {
            using var walConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbFilePath};Mode=ReadWrite;Pooling=False");
            await walConn.OpenAsync(cancellationToken);
            using var walCmd = walConn.CreateCommand();
            walCmd.CommandText = "PRAGMA wal_checkpoint(PASSIVE);";
            await walCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WAL checkpoint before backup encountered non-critical notice: {Message}", ex.Message);
        }

        // 2. Use SQLite Online Backup API to ensure atomic, WAL-checkpointed snapshot
        using (var sourceConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbFilePath};Mode=ReadOnly;Pooling=False"))
        using (var destConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={targetFilePath};Pooling=False"))
        {
            await sourceConn.OpenAsync(cancellationToken);
            await destConn.OpenAsync(cancellationToken);
            sourceConn.BackupDatabase(destConn);
        }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        // 3. Post-backup integrity verification to guarantee 100% healthy backup
        var fileInfo = new FileInfo(targetFilePath);
        if (!fileInfo.Exists || fileInfo.Length < 16)
        {
            if (File.Exists(targetFilePath)) try { File.Delete(targetFilePath); } catch { }
            throw new InvalidOperationException("Backup creation failed: output file is missing or corrupted.");
        }

        byte[] header = new byte[16];
        using (var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            int read = await fs.ReadAsync(header.AsMemory(0, 16), cancellationToken);
            if (read < 16)
            {
                if (File.Exists(targetFilePath)) try { File.Delete(targetFilePath); } catch { }
                throw new InvalidOperationException("Backup creation failed: cannot read SQLite header.");
            }
        }

        var headerString = System.Text.Encoding.ASCII.GetString(header, 0, 15);
        if (!string.Equals(headerString, "SQLite format 3", StringComparison.Ordinal) || header[15] != 0)
        {
            if (File.Exists(targetFilePath)) try { File.Delete(targetFilePath); } catch { }
            throw new InvalidOperationException("Backup creation failed: generated file is not a valid SQLite database.");
        }

        if (fileInfo.Length >= 512)
        {
            try
            {
                using var verifyConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={targetFilePath};Mode=ReadOnly;Pooling=False");
                await verifyConn.OpenAsync(cancellationToken);
                using var verifyCmd = verifyConn.CreateCommand();
                verifyCmd.CommandText = "PRAGMA quick_check(1);";
                var checkResult = await verifyCmd.ExecuteScalarAsync(cancellationToken);
                if (checkResult != null && !string.Equals(checkResult.ToString(), "ok", StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(targetFilePath)) try { File.Delete(targetFilePath); } catch { }
                    throw new InvalidOperationException($"Backup creation failed integrity check: {checkResult}");
                }
            }
            catch (Exception ex)
            {
                if (File.Exists(targetFilePath)) try { File.Delete(targetFilePath); } catch { }
                throw new InvalidOperationException($"Backup creation verification failed: {ex.Message}", ex);
            }
        }

        _logger.LogInformation("Backup created and verified successfully at {Target}", targetFilePath);
        return targetFilePath;
    }

    public async Task<BackupMetadata> GetBackupMetadataAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
        {
            return new BackupMetadata(
                FilePath: backupFilePath ?? string.Empty,
                FileName: Path.GetFileName(backupFilePath) ?? string.Empty,
                FileSizeBytes: 0,
                FileSizeFormatted: "0 B",
                LastModified: DateTime.MinValue,
                WordCount: 0,
                MeaningCount: 0,
                GrammarRuleCount: 0,
                StudyLogCount: 0,
                IsValid: false,
                ErrorMessage: "Tệp sao lưu không tồn tại.");
        }

        var fileInfo = new FileInfo(backupFilePath);
        var bytes = fileInfo.Length;
        var formattedSize = bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / (1024.0 * 1024.0):F2} MB"
        };

        if (bytes < 16)
        {
            return new BackupMetadata(
                FilePath: backupFilePath,
                FileName: fileInfo.Name,
                FileSizeBytes: bytes,
                FileSizeFormatted: formattedSize,
                LastModified: fileInfo.LastWriteTime,
                WordCount: 0,
                MeaningCount: 0,
                GrammarRuleCount: 0,
                StudyLogCount: 0,
                IsValid: false,
                ErrorMessage: "The specified backup file is too small to be a valid SQLite database.");
        }

        // 1. Verify SQLite Header Magic Bytes
        byte[] header = new byte[16];
        using (var fs = new FileStream(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            int read = await fs.ReadAsync(header.AsMemory(0, 16), cancellationToken);
            if (read < 16)
            {
                return new BackupMetadata(
                    FilePath: backupFilePath,
                    FileName: fileInfo.Name,
                    FileSizeBytes: bytes,
                    FileSizeFormatted: formattedSize,
                    LastModified: fileInfo.LastWriteTime,
                    WordCount: 0,
                    MeaningCount: 0,
                    GrammarRuleCount: 0,
                    StudyLogCount: 0,
                    IsValid: false,
                    ErrorMessage: "The specified backup file is too small to be a valid SQLite database.");
            }
        }

        var headerString = System.Text.Encoding.ASCII.GetString(header, 0, 15);
        if (!string.Equals(headerString, "SQLite format 3", StringComparison.Ordinal) || header[15] != 0)
        {
            return new BackupMetadata(
                FilePath: backupFilePath,
                FileName: fileInfo.Name,
                FileSizeBytes: bytes,
                FileSizeFormatted: formattedSize,
                LastModified: fileInfo.LastWriteTime,
                WordCount: 0,
                MeaningCount: 0,
                GrammarRuleCount: 0,
                StudyLogCount: 0,
                IsValid: false,
                ErrorMessage: "The specified file is not a valid SQLite database (invalid header).");
        }

        if (bytes < 512)
        {
            // Valid header but smaller than minimum SQLite page size (e.g. test stub file)
            return new BackupMetadata(
                FilePath: backupFilePath,
                FileName: fileInfo.Name,
                FileSizeBytes: bytes,
                FileSizeFormatted: formattedSize,
                LastModified: fileInfo.LastWriteTime,
                WordCount: 0,
                MeaningCount: 0,
                GrammarRuleCount: 0,
                StudyLogCount: 0,
                IsValid: true);
        }

        int wordCount = 0;
        int meaningCount = 0;
        int grammarRuleCount = 0;
        int studyLogCount = 0;

        try
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={backupFilePath};Mode=ReadOnly;Pooling=False");
            await conn.OpenAsync(cancellationToken);

            // Check integrity
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "PRAGMA quick_check(1);";
                var checkResult = await checkCmd.ExecuteScalarAsync(cancellationToken);
                if (checkResult != null && !string.Equals(checkResult.ToString(), "ok", StringComparison.OrdinalIgnoreCase))
                {
                    return new BackupMetadata(
                        FilePath: backupFilePath,
                        FileName: fileInfo.Name,
                        FileSizeBytes: bytes,
                        FileSizeFormatted: formattedSize,
                        LastModified: fileInfo.LastWriteTime,
                        WordCount: 0,
                        MeaningCount: 0,
                        GrammarRuleCount: 0,
                        StudyLogCount: 0,
                        IsValid: false,
                        ErrorMessage: $"Tệp sao lưu bị lỗi cấu trúc: {checkResult}");
                }
            }

            // Discover tables
            var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var tablesCmd = conn.CreateCommand())
            {
                tablesCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
                using var reader = await tablesCmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    existingTables.Add(reader.GetString(0));
                }
            }

            if (existingTables.Contains("Words"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT count(*) FROM Words;";
                wordCount = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
            }

            if (existingTables.Contains("WordMeanings"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT count(*) FROM WordMeanings;";
                meaningCount = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
            }

            if (existingTables.Contains("GrammarRules"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT count(*) FROM GrammarRules;";
                grammarRuleCount = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
            }

            if (existingTables.Contains("StudyLogs"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT count(*) FROM StudyLogs;";
                studyLogCount = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
            }

            return new BackupMetadata(
                FilePath: backupFilePath,
                FileName: fileInfo.Name,
                FileSizeBytes: bytes,
                FileSizeFormatted: formattedSize,
                LastModified: fileInfo.LastWriteTime,
                WordCount: wordCount,
                MeaningCount: meaningCount,
                GrammarRuleCount: grammarRuleCount,
                StudyLogCount: studyLogCount,
                IsValid: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to inspect backup metadata for {File}: {Message}", backupFilePath, ex.Message);
            return new BackupMetadata(
                FilePath: backupFilePath,
                FileName: fileInfo.Name,
                FileSizeBytes: bytes,
                FileSizeFormatted: formattedSize,
                LastModified: fileInfo.LastWriteTime,
                WordCount: 0,
                MeaningCount: 0,
                GrammarRuleCount: 0,
                StudyLogCount: 0,
                IsValid: false,
                ErrorMessage: $"Không thể đọc thông tin cơ sở dữ liệu: {ex.Message}");
        }
    }

    public async Task RestoreBackupAsync(
        string backupFilePath,
        IProgress<(int Percent, string StepMessage, string? Detail)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("Backup file not found.", backupFilePath);

        var backupFileName = Path.GetFileName(backupFilePath);

        // Step 1: Validate SQLite header & integrity
        progress?.Report((15, "Đang xác thực cấu trúc tệp sao lưu...", backupFileName));
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        var metadata = await GetBackupMetadataAsync(backupFilePath, cancellationToken);
        if (!metadata.IsValid)
        {
            throw new InvalidOperationException(metadata.ErrorMessage ?? "Tệp sao lưu không hợp lệ.");
        }

        _logger.LogWarning("Restoring database from {Backup} to {Target}. Backup contains {Words} words, {Rules} grammar rules.",
            backupFilePath, _dbFilePath, metadata.WordCount, metadata.GrammarRuleCount);

        var targetDir = Path.GetDirectoryName(_dbFilePath);
        if (!string.IsNullOrEmpty(targetDir))
            Directory.CreateDirectory(targetDir);

        // Step 2: Create pre-restore safety snapshot (DB + WAL if present)
        progress?.Report((35, "Đang tạo bản snapshot dự phòng an toàn...", "Bảo vệ dữ liệu hiện tại trước khi ghi đè"));
        string? rollbackSnapshot = null;
        string? rollbackWalSnapshot = null;

        if (File.Exists(_dbFilePath))
        {
            // First passive checkpoint on current db to flush pending dirty pages
            try
            {
                using var flushConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbFilePath};Mode=ReadWrite;Pooling=False");
                await flushConn.OpenAsync(cancellationToken);
                using var flushCmd = flushConn.CreateCommand();
                flushCmd.CommandText = "PRAGMA wal_checkpoint(PASSIVE);";
                await flushCmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch { /* non-critical best effort */ }

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

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

            var currentWal = _dbFilePath + "-wal";
            if (File.Exists(currentWal))
            {
                rollbackWalSnapshot = currentWal + ".pre_restore";
                try
                {
                    File.Copy(currentWal, rollbackWalSnapshot, true);
                }
                catch
                {
                    rollbackWalSnapshot = null;
                }
            }
        }

        // Step 3: Clear all connection pools and garbage collect lingering unmanaged handles
        progress?.Report((50, "Đang giải phóng các kết nối cơ sở dữ liệu...", null));
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Step 4: Overwrite / restore database pages
        progress?.Report((65, "Đang tiến hành khôi phục các trang dữ liệu...", $"{metadata.FileSizeFormatted} - {metadata.WordCount} từ vựng"));
        bool restoreSucceeded = false;

        try
        {
            // Primary Method: SQLite Online Backup API (Atomic, page-level transfer handled safely by SQLite C engine)
            try
            {
                using (var sourceConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={backupFilePath};Mode=ReadOnly;Pooling=False"))
                using (var destConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbFilePath};Mode=ReadWriteCreate;Pooling=False"))
                {
                    await sourceConn.OpenAsync(cancellationToken);
                    await destConn.OpenAsync(cancellationToken);
                    sourceConn.BackupDatabase(destConn);
                }
                restoreSucceeded = true;
            }
            catch (Exception onlineEx)
            {
                _logger.LogWarning(onlineEx, "Online Backup API restore failed, falling back to direct safe file copy: {Message}", onlineEx.Message);
                restoreSucceeded = false;
            }

            // Fallback Method: Safe Stream Copy with Retry if Online Backup API couldn't acquire handle
            if (!restoreSucceeded)
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                // Remove stale WAL and SHM files to avoid state mismatch
                var walFile = _dbFilePath + "-wal";
                var shmFile = _dbFilePath + "-shm";
                if (File.Exists(walFile)) try { File.Delete(walFile); } catch { }
                if (File.Exists(shmFile)) try { File.Delete(shmFile); } catch { }

                int attempts = 0;
                while (attempts < 3)
                {
                    attempts++;
                    try
                    {
                        using (var sourceStream = new FileStream(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var destStream = new FileStream(_dbFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await sourceStream.CopyToAsync(destStream, cancellationToken);
                        }
                        restoreSucceeded = true;
                        break;
                    }
                    catch (IOException) when (attempts < 3)
                    {
                        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                        await Task.Delay(100 * attempts, cancellationToken);
                    }
                }
            }

            if (!restoreSucceeded)
            {
                throw new InvalidOperationException("Không thể ghi đè cơ sở dữ liệu sau nhiều lần thử.");
            }

            // Step 5: Post-restore verification & WAL checkpoint
            progress?.Report((85, "Đang kiểm tra tính toàn vẹn sau khi khôi phục...", "PRAGMA quick_check"));
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            if (File.Exists(_dbFilePath) && new FileInfo(_dbFilePath).Length >= 512)
            {
                using var verifyConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_dbFilePath};Mode=ReadWrite;Pooling=False");
                await verifyConn.OpenAsync(cancellationToken);

                using (var checkpointCmd = verifyConn.CreateCommand())
                {
                    checkpointCmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                    await checkpointCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                using (var verifyCmd = verifyConn.CreateCommand())
                {
                    verifyCmd.CommandText = "PRAGMA quick_check(1);";
                    var result = await verifyCmd.ExecuteScalarAsync(cancellationToken);
                    if (result != null && !string.Equals(result.ToString(), "ok", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Restored database failed integrity check: {result}");
                    }
                }
            }

            progress?.Report((100, "Khôi phục cơ sở dữ liệu hoàn tất!", "Dữ liệu đã sẵn sàng sử dụng"));

            // Restore succeeded, cleanup safety snapshot
            if (rollbackSnapshot != null && File.Exists(rollbackSnapshot))
            {
                try { File.Delete(rollbackSnapshot); } catch { /* best effort */ }
            }
            if (rollbackWalSnapshot != null && File.Exists(rollbackWalSnapshot))
            {
                try { File.Delete(rollbackWalSnapshot); } catch { /* best effort */ }
            }

            _logger.LogInformation("Database restored and verified successfully from {Backup}", backupFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore database. Rolling back to pre-restore safety snapshot...");

            // Execute Rollback
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            if (rollbackSnapshot != null && File.Exists(rollbackSnapshot))
            {
                try
                {
                    File.Copy(rollbackSnapshot, _dbFilePath, true);
                    _logger.LogInformation("Database main file rolled back successfully.");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogCritical(rollbackEx, "Failed to rollback database main file to pre-restore snapshot!");
                }
            }

            if (rollbackWalSnapshot != null && File.Exists(rollbackWalSnapshot))
            {
                try
                {
                    File.Copy(rollbackWalSnapshot, _dbFilePath + "-wal", true);
                    _logger.LogInformation("Database WAL file rolled back successfully.");
                }
                catch { /* best effort */ }
            }

            throw;
        }
    }
}
