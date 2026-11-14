using FluentAssertions;
using Lexora.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexora.Tests.Unit;

public class AutoBackupRotationTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _dummyDbPath;

    public AutoBackupRotationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "Lexora_AutoBackupTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _dummyDbPath = Path.Combine(_testDir, "lexora.db");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task RotateBackupsAsync_WhenMoreThanMaxBackups_PrunesOldestToRetainLimit()
    {
        // Arrange: Create 8 timestamped auto-backup files
        var autoBackupDir = Path.Combine(_testDir, "backups", "auto");
        Directory.CreateDirectory(autoBackupDir);

        var createdFiles = new List<string>();
        var baseTime = DateTime.UtcNow.AddDays(-10);

        for (int i = 0; i < 8; i++)
        {
            var fileName = $"lexora_autobackup_202611{i + 1:D2}_120000.db";
            var filePath = Path.Combine(autoBackupDir, fileName);
            File.WriteAllText(filePath, "dummy sqlite content");
            File.SetLastWriteTimeUtc(filePath, baseTime.AddDays(i));
            createdFiles.Add(filePath);
        }

        var service = new DatabaseBackupService(_dummyDbPath, NullLogger<DatabaseBackupService>.Instance);

        // Act: Rotate to retain max 5 backups
        var deleted = await service.RotateBackupsAsync(autoBackupDir, maxRetainedBackups: 5);

        // Assert
        deleted.Should().HaveCount(3);
        var remaining = Directory.GetFiles(autoBackupDir, "lexora_autobackup_*.db");
        remaining.Should().HaveCount(5);

        // Verify the 5 newest remain
        var remainingNames = remaining.Select(Path.GetFileName).ToList();
        remainingNames.Should().Contain("lexora_autobackup_20261108_120000.db");
        remainingNames.Should().Contain("lexora_autobackup_20261107_120000.db");
        remainingNames.Should().Contain("lexora_autobackup_20261106_120000.db");
        remainingNames.Should().Contain("lexora_autobackup_20261105_120000.db");
        remainingNames.Should().Contain("lexora_autobackup_20261104_120000.db");

        // Oldest 3 should have been deleted
        remainingNames.Should().NotContain("lexora_autobackup_20261101_120000.db");
        remainingNames.Should().NotContain("lexora_autobackup_20261102_120000.db");
        remainingNames.Should().NotContain("lexora_autobackup_20261103_120000.db");
    }

    [Fact]
    public async Task RotateBackupsAsync_WhenFewerThanMaxBackups_DeletesNothing()
    {
        // Arrange: Create 3 auto-backup files
        var autoBackupDir = Path.Combine(_testDir, "backups", "auto");
        Directory.CreateDirectory(autoBackupDir);

        for (int i = 0; i < 3; i++)
        {
            var fileName = $"lexora_autobackup_202611{i + 1:D2}_120000.db";
            var filePath = Path.Combine(autoBackupDir, fileName);
            File.WriteAllText(filePath, "dummy sqlite content");
        }

        var service = new DatabaseBackupService(_dummyDbPath, NullLogger<DatabaseBackupService>.Instance);

        // Act
        var deleted = await service.RotateBackupsAsync(autoBackupDir, maxRetainedBackups: 5);

        // Assert
        deleted.Should().BeEmpty();
        var remaining = Directory.GetFiles(autoBackupDir, "lexora_autobackup_*.db");
        remaining.Should().HaveCount(3);
    }

    [Fact]
    public async Task EnsureAutoBackupAsync_WhenDatabaseMissing_ReturnsNullGracefully()
    {
        // Arrange
        var missingDbPath = Path.Combine(_testDir, "non_existent.db");
        var service = new DatabaseBackupService(missingDbPath, NullLogger<DatabaseBackupService>.Instance);

        // Act
        var result = await service.EnsureAutoBackupAsync(maxRetainedBackups: 5);

        // Assert
        result.Should().BeNull();
    }
}
