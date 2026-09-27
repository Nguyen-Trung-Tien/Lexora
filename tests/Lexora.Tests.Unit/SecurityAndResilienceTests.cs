using System.Text;
using FluentAssertions;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class SecurityAndResilienceTests : IDisposable
{
    private readonly string _tempDirectory;

    public SecurityAndResilienceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "Lexora_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    [Theory]
    [InlineData("../../evil", "uk")]
    [InlineData("..\\..\\evil", "uk")]
    [InlineData("word", "../../uk")]
    [InlineData("word", "..\\..\\uk")]
    [InlineData("test/../../escape", "us")]
    public async Task LocalAudioStorageService_PathTraversal_ShouldRemainConfinedToCacheDirectory(string term, string accent)
    {
        // Arrange
        var cacheDir = Path.Combine(_tempDirectory, "audio_cache");
        var storageService = new LocalAudioStorageService(cacheDir);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake mp3 data"));

        // Act
        var cachedPath = await storageService.CacheAudioAsync(term, accent, stream);

        // Assert
        if (cachedPath != null)
        {
            var fullCacheDir = Path.GetFullPath(cacheDir);
            var fullCachedPath = Path.GetFullPath(cachedPath);
            fullCachedPath.Should().StartWith(fullCacheDir, "cached files must never escape cache directory");
            File.Exists(cachedPath).Should().BeTrue();
        }
    }

    [Fact]
    public async Task DatabaseBackupService_Restore_InvalidHeader_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbFile = Path.Combine(_tempDirectory, "lexora.db");
        File.WriteAllText(dbFile, "initial db");

        var maliciousBackup = Path.Combine(_tempDirectory, "malicious.db");
        File.WriteAllText(maliciousBackup, "This is not a SQLite database file at all!");

        var backupService = new DatabaseBackupService(dbFile, NullLogger<DatabaseBackupService>.Instance);

        // Act
        var act = async () => await backupService.RestoreBackupAsync(maliciousBackup);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a valid SQLite database*");
    }

    [Fact]
    public async Task DatabaseBackupService_Restore_ShortFile_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbFile = Path.Combine(_tempDirectory, "lexora.db");
        File.WriteAllText(dbFile, "initial db");

        var shortBackup = Path.Combine(_tempDirectory, "short.db");
        File.WriteAllBytes(shortBackup, new byte[] { 0x01, 0x02, 0x03 });

        var backupService = new DatabaseBackupService(dbFile, NullLogger<DatabaseBackupService>.Instance);

        // Act
        var act = async () => await backupService.RestoreBackupAsync(shortBackup);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*too small*");
    }

    [Fact]
    public async Task DatabaseBackupService_Restore_ValidSQLiteHeader_Succeeds()
    {
        // Arrange
        var dbFile = Path.Combine(_tempDirectory, "lexora.db");
        File.WriteAllText(dbFile, "initial db");

        // Write 16-byte SQLite header: "SQLite format 3\0"
        var validBackup = Path.Combine(_tempDirectory, "valid.db");
        byte[] validHeader = new byte[100];
        Encoding.ASCII.GetBytes("SQLite format 3").CopyTo(validHeader, 0);
        validHeader[15] = 0x00;
        File.WriteAllBytes(validBackup, validHeader);

        var backupService = new DatabaseBackupService(dbFile, NullLogger<DatabaseBackupService>.Instance);

        // Act
        await backupService.RestoreBackupAsync(validBackup);

        // Assert
        File.Exists(dbFile).Should().BeTrue();
        var restoredBytes = await File.ReadAllBytesAsync(dbFile);
        restoredBytes.Should().Equal(validHeader);
    }

    [Fact]
    public async Task ExportImportService_ImportFromCsv_WithMultilineQuotedFields_ParsesCorrectly()
    {
        // Arrange
        var vocabRepoMock = new Mock<IVocabularyRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        vocabRepoMock.Setup(r => r.GetAllTopicsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Topic>());
        vocabRepoMock.Setup(r => r.GetAllExamTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExamTag>());
        vocabRepoMock.Setup(r => r.GetByTermAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        var service = new ExportImportService(vocabRepoMock.Object, unitOfWorkMock.Object);

        // CSV containing a quoted example with line breaks inside quotes
        var csvContent =
            "Term,Level,PartOfSpeech,PhoneticUk,PhoneticUs,MeaningsVi,Examples,Topics,ExamTags,Notes\n" +
            "\"ephemeral\",B2,Adjective,\"/ɪˈfem.ər.əl/\",\"/ɪˈfem.ɚ.əl/\",\"phù du, sớm nở tối tàn\",\"Line 1 of example\nLine 2 of example => Vi line 1\nVi line 2\",Technology,IELTSTag,\"Note with\nmultiple\nlines\"";

        // Act
        var result = await service.ImportFromCsvAsync(csvContent);

        // Assert
        result.TotalImported.Should().Be(1);
        result.TotalSkipped.Should().Be(0);
        result.Errors.Should().BeEmpty();

        vocabRepoMock.Verify(r => r.AddAsync(It.Is<Word>(w =>
            w.Term == "ephemeral" &&
            w.Notes != null &&
            w.Notes.Contains("multiple")
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportImportService_ImportFromCsv_WithDuplicatesInSameBatch_DeduplicatesCorrectly()
    {
        // Arrange
        var vocabRepoMock = new Mock<IVocabularyRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        vocabRepoMock.Setup(r => r.GetAllTopicsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Topic>());
        vocabRepoMock.Setup(r => r.GetAllExamTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExamTag>());
        vocabRepoMock.Setup(r => r.GetByTermAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        var service = new ExportImportService(vocabRepoMock.Object, unitOfWorkMock.Object);

        // CSV has two rows for 'resilient' (one upper, one lower)
        var csvContent =
            "Term,Level,PartOfSpeech,PhoneticUk,PhoneticUs,MeaningsVi,Examples,Topics,ExamTags,Notes\n" +
            "resilient,B2,Adjective,,,kiên cường,,,,,\n" +
            "RESILIENT,B2,Adjective,,,kiên cường,,,,,";

        // Act
        var result = await service.ImportFromCsvAsync(csvContent);

        // Assert
        result.TotalImported.Should().Be(1);
        result.TotalSkipped.Should().Be(1);
        vocabRepoMock.Verify(r => r.AddAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void SqliteConnectionStringBuilder_WhenConfiguredWithResilienceFlags_ContainsBusyTimeout()
    {
        // Arrange
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(_tempDirectory, "lexora.db"),
            Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate,
            Cache = Microsoft.Data.Sqlite.SqliteCacheMode.Shared,
            DefaultTimeout = 15
        };

        // Assert
        builder.ConnectionString.Should().Contain("Cache=Shared");
        builder.DefaultTimeout.Should().Be(15);
    }

    [Theory]
    [InlineData("backup.exe")]
    [InlineData("backup.bat")]
    [InlineData("backup.sh")]
    [InlineData("backup.dll")]
    [InlineData("backup.txt")]
    public async Task DatabaseBackupService_CreateBackupToFile_InvalidExtension_ThrowsArgumentException(string invalidFileName)
    {
        // Arrange
        var dbFile = Path.Combine(_tempDirectory, "lexora.db");
        File.WriteAllText(dbFile, "db dummy content");
        var backupService = new DatabaseBackupService(dbFile, NullLogger<DatabaseBackupService>.Instance);
        var targetPath = Path.Combine(_tempDirectory, invalidFileName);

        // Act
        var act = async () => await backupService.CreateBackupToFileAsync(targetPath);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid backup file extension*");
    }

    [Fact]
    public async Task DatabaseBackupService_CreateBackupToFile_EmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var dbFile = Path.Combine(_tempDirectory, "lexora.db");
        var backupService = new DatabaseBackupService(dbFile, NullLogger<DatabaseBackupService>.Instance);

        // Act
        var act = async () => await backupService.CreateBackupToFileAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Target file path cannot be empty*");
    }

    [Fact]
    public async Task LocalAudioStorageService_CacheAudioAsync_Exceeds10MB_ThrowsArgumentException()
    {
        // Arrange
        var cacheDir = Path.Combine(_tempDirectory, "audio_cache_limit");
        var storageService = new LocalAudioStorageService(cacheDir);

        // Create a fake stream reporting > 10MB
        var mockStream = new Mock<Stream>();
        mockStream.Setup(s => s.CanSeek).Returns(true);
        mockStream.Setup(s => s.Length).Returns(11 * 1024 * 1024); // 11MB

        // Act
        var act = async () => await storageService.CacheAudioAsync("oversized", "us", mockStream.Object);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*exceeds maximum allowable size*");
    }

    [Fact]
    public async Task ExportImportService_ImportFromCsv_Exceeding20MB_ReturnsPayloadLimitError()
    {
        // Arrange
        var vocabRepoMock = new Mock<IVocabularyRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var service = new ExportImportService(vocabRepoMock.Object, unitOfWorkMock.Object);

        // Create a string of length > 20_000_000
        var oversizedCsv = new string('A', 20_000_001);

        // Act
        var result = await service.ImportFromCsvAsync(oversizedCsv);

        // Assert
        result.TotalImported.Should().Be(0);
        result.Errors.Should().Contain(e => e.Contains("exceeds maximum allowed size"));
    }
}

