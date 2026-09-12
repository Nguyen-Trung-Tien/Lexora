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
        File.WriteAllBytes(_mockDbPath, [0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x20, 0x66, 0x6F, 0x72, 0x6D, 0x61, 0x74, 0x20, 0x33, 0x00]); // SQLite header magic bytes

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

        var sourceBytes = await File.ReadAllBytesAsync(_mockDbPath);
        var backupBytes = await File.ReadAllBytesAsync(backupFilePath);
        backupBytes.Should().Equal(sourceBytes);
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
        var headerBytes = System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0");
        var newPayload = headerBytes.Concat(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }).ToArray();
        await File.WriteAllBytesAsync(customBackupPath, newPayload);

        // Act
        await _backupService.RestoreBackupAsync(customBackupPath);

        // Assert
        var restoredBytes = await File.ReadAllBytesAsync(_mockDbPath);
        restoredBytes.Should().Equal(newPayload);
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
