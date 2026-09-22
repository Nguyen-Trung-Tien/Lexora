using FluentAssertions;
using Lexora.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexora.Tests.Integration;

public class DatabaseBackupServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _mockDbPath;
    private readonly DatabaseBackupService _backupService;

    public DatabaseBackupServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "Lexora_Backup_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        _mockDbPath = Path.Combine(_tempDirectory, "mock_lexora.db");
        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_mockDbPath};Pooling=False;"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT); INSERT INTO TestTable VALUES (1, 'Lexora');";
            cmd.ExecuteNonQuery();
        }

        _backupService = new DatabaseBackupService(_mockDbPath, NullLogger<DatabaseBackupService>.Instance);
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldCreateTimestampedBackupFile_WhenDbExists()
    {
        // Arrange
        var backupDestDir = Path.Combine(_tempDirectory, "Backups");

        // Act
        var backupFilePath = await _backupService.CreateBackupAsync(backupDestDir);

        // Assert
        File.Exists(backupFilePath).Should().BeTrue();
        Path.GetDirectoryName(backupFilePath).Should().Be(backupDestDir);
        Path.GetFileName(backupFilePath).Should().StartWith("lexora_backup_").And.EndWith(".db");

        // Verify backup contains the same data and valid SQLite header
        using (var backupConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={backupFilePath};Mode=ReadOnly;Pooling=False;"))
        {
            await backupConn.OpenAsync();
            using var cmd = backupConn.CreateCommand();
            cmd.CommandText = "SELECT Name FROM TestTable WHERE Id = 1;";
            var name = (string?)await cmd.ExecuteScalarAsync();
            name.Should().Be("Lexora");
        }
    }

    [Fact]
    public async Task CreateBackupToFileAsync_ShouldCreateSpecifiedFile_WhenDbExists()
    {
        // Arrange
        var customTarget = Path.Combine(_tempDirectory, "custom_named_backup.db");

        // Act
        var resultPath = await _backupService.CreateBackupToFileAsync(customTarget);

        // Assert
        resultPath.Should().Be(customTarget);
        File.Exists(customTarget).Should().BeTrue();

        using (var backupConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={customTarget};Mode=ReadOnly;Pooling=False;"))
        {
            await backupConn.OpenAsync();
            using var cmd = backupConn.CreateCommand();
            cmd.CommandText = "SELECT Name FROM TestTable WHERE Id = 1;";
            var name = (string?)await cmd.ExecuteScalarAsync();
            name.Should().Be("Lexora");
        }
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldThrowFileNotFoundException_WhenSourceDbMissing()
    {
        // Arrange
        var nonExistentDb = Path.Combine(_tempDirectory, "missing.db");
        var service = new DatabaseBackupService(nonExistentDb, NullLogger<DatabaseBackupService>.Instance);

        // Act
        var act = async () => await service.CreateBackupAsync(_tempDirectory);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task RestoreBackupAsync_ShouldOverwriteTargetDatabase_WhenBackupValid()
    {
        // Arrange
        var customBackupPath = Path.Combine(_tempDirectory, "restore_test.db");
        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={customBackupPath};Pooling=False;"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE RestoredTable (Val TEXT); INSERT INTO RestoredTable VALUES ('RestoredData');";
            cmd.ExecuteNonQuery();
        }

        // Act
        await _backupService.RestoreBackupAsync(customBackupPath);

        // Assert
        using (var checkConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_mockDbPath};Mode=ReadOnly;Pooling=False;"))
        {
            await checkConn.OpenAsync();
            using var cmd = checkConn.CreateCommand();
            cmd.CommandText = "SELECT Val FROM RestoredTable;";
            var val = (string?)await cmd.ExecuteScalarAsync();
            val.Should().Be("RestoredData");
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_ShouldThrowFileNotFoundException_WhenBackupFileMissing()
    {
        // Arrange
        var nonExistentBackup = Path.Combine(_tempDirectory, "ghost_backup.db");

        // Act
        var act = async () => await _backupService.RestoreBackupAsync(nonExistentBackup);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public void DatabasePath_Property_ShouldReturnConfiguredDbPath()
    {
        _backupService.DatabasePath.Should().Be(_mockDbPath);
    }

    public void Dispose()
    {
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore temp cleanup errors
        }
    }
}
