using System.Security;
using FluentAssertions;
using Lexora.Application.Common;
using Xunit;

namespace Lexora.Tests.Unit;

public class SafePathHelperTests
{
    [Fact]
    public void ValidatePathWithinDirectory_WithValidSubPath_ShouldReturnCanonicalPath()
    {
        // Arrange
        var baseDir = Path.Combine(Path.GetTempPath(), "lexora_safe_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(baseDir);
        try
        {
            var targetPath = Path.Combine(baseDir, "subfolder", "file.db");

            // Act
            var canonical = SafePathHelper.ValidatePathWithinDirectory(targetPath, baseDir);

            // Assert
            canonical.Should().StartWith(Path.GetFullPath(baseDir));
            canonical.Should().EndWith("file.db");
        }
        finally
        {
            if (Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, recursive: true);
            }
        }
    }

    [Fact]
    public void ValidatePathWithinDirectory_WithPathTraversal_ShouldThrowSecurityException()
    {
        // Arrange
        var baseDir = Path.Combine(Path.GetTempPath(), "lexora_safe_test_" + Guid.NewGuid().ToString("N"));
        var maliciousPath = Path.Combine(baseDir, "..", "..", "windows", "system32", "calc.exe");

        // Act & Assert
        var act = () => SafePathHelper.ValidatePathWithinDirectory(maliciousPath, baseDir);
        act.Should().Throw<SecurityException>()
            .WithMessage("*traversal*");
    }

    [Theory]
    [InlineData("target.txt", "")]
    [InlineData("target.txt", "   ")]
    [InlineData("target.txt", null)]
    public void ValidatePathWithinDirectory_WithInvalidBaseDir_ShouldThrowArgumentException(string targetPath, string? baseDir)
    {
        // Act & Assert
        var act = () => SafePathHelper.ValidatePathWithinDirectory(targetPath, baseDir!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("", "C:\\base")]
    [InlineData("   ", "C:\\base")]
    [InlineData(null, "C:\\base")]
    public void ValidatePathWithinDirectory_WithInvalidTargetPath_ShouldThrowArgumentException(string? targetPath, string baseDir)
    {
        // Act & Assert
        var act = () => SafePathHelper.ValidatePathWithinDirectory(targetPath!, baseDir);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SanitizeFileName_WithInvalidChars_ShouldStripOrReplaceThem()
    {
        // Arrange
        var rawName = "my:bad*file?name<test>|name.db";

        // Act
        var sanitized = SafePathHelper.SanitizeFileName(rawName);

        // Assert
        sanitized.Should().NotContainAny(":", "*", "?", "<", ">", "|");
        sanitized.Should().EndWith(".db");
    }

    [Fact]
    public void SanitizeFileName_WithOnlyInvalidChars_ShouldReturnFallback()
    {
        // Arrange
        var rawName = "<>:\"|?*";

        // Act
        var sanitized = SafePathHelper.SanitizeFileName(rawName);

        // Assert
        sanitized.Should().Be("unnamed_file");
    }

    [Fact]
    public void SanitizeFileName_WithCleanName_ShouldReturnTrimmedName()
    {
        // Arrange
        var rawName = "  valid_backup_20261114.db  ";

        // Act
        var sanitized = SafePathHelper.SanitizeFileName(rawName);

        // Assert
        sanitized.Should().Be("valid_backup_20261114.db");
    }
}
