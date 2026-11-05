using Lexora.Infrastructure.Services;
using Xunit;

namespace Lexora.Tests.Unit;

public class ReleaseNotesServiceTests : IDisposable
{
    private readonly string _tempDir;

    public ReleaseNotesServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "Lexora_ReleaseNotes_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup failure in temp directory
        }
    }

    [Fact]
    public async Task GetAllReleaseNotesAsync_WhenFilesExist_ReturnsSortedDescending()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "v1.4.0.md"), "# Lexora v1.4.0\n> Release Date: 2026-10-27\n\n## 🌟 Highlights\nSummary for 1.4.0");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "v1.6.1.md"), "# Lexora v1.6.1\n> Release Date: 2026-11-05\n\n## 🌟 Highlights\nSummary for 1.6.1");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "v1.5.0.md"), "# Lexora v1.5.0\n> Release Date: 2026-11-03\n\n## 🌟 Highlights\nSummary for 1.5.0");

        var service = new ReleaseNotesService(_tempDir);

        // Act
        var result = await service.GetAllReleaseNotesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("1.6.1", result[0].Version);
        Assert.Equal("1.5.0", result[1].Version);
        Assert.Equal("1.4.0", result[2].Version);
    }

    [Fact]
    public async Task GetReleaseNoteAsync_WithVersionPrefix_ReturnsMatchedNote()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "v1.6.1.md"), "# Lexora v1.6.1 — Learning Pathway\n> Release Date: 2026-11-05\n\n## 🌟 Highlights\nComprehensive system optimizations.");
        var service = new ReleaseNotesService(_tempDir);

        // Act
        var noteWithV = await service.GetReleaseNoteAsync("v1.6.1");
        var noteWithoutV = await service.GetReleaseNoteAsync("1.6.1");

        // Assert
        Assert.NotNull(noteWithV);
        Assert.NotNull(noteWithoutV);
        Assert.Equal("1.6.1", noteWithV.Version);
        Assert.Equal(noteWithV.Title, noteWithoutV.Title);
        Assert.Equal("2026-11-05", noteWithV.ReleaseDate);
        Assert.Contains("Comprehensive system optimizations.", noteWithV.Summary);
    }

    [Fact]
    public async Task GetCurrentVersionReleaseNoteAsync_WhenCurrentVersionMatches_ReturnsIsCurrentTrue()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "v1.6.1.md"), "# Lexora v1.6.1\n> Release Date: 2026-11-05\n\n## 🌟 Highlights\nCurrent active version.");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "v1.5.0.md"), "# Lexora v1.5.0\n> Release Date: 2026-11-03\n\n## 🌟 Highlights\nPast version.");

        var service = new ReleaseNotesService(_tempDir);

        // Act
        var currentNote = await service.GetCurrentVersionReleaseNoteAsync();

        // Assert
        Assert.NotNull(currentNote);
        Assert.True(currentNote.IsCurrent);
        Assert.Equal("1.6.1", currentNote.Version);
    }

    [Fact]
    public async Task GetAllReleaseNotesAsync_WhenDirectoryDoesNotExist_ReturnsBuiltinFallback()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid().ToString("N"));
        var service = new ReleaseNotesService(nonExistentDir);

        // Act
        var result = await service.GetAllReleaseNotesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, n => n.Version == "1.6.1");
    }
}

