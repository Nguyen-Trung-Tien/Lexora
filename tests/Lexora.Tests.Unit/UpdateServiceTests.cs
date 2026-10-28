using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Lexora.Tests.Unit;

public class UpdateServiceTests
{
    [Theory]
    [InlineData("1.0.0", "1.1.0", true)]
    [InlineData("1.0.0", "v1.1.0", true)]
    [InlineData("1.2.0", "v1.2.1", true)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("v1.0.0", "1.0.0", false)]
    [InlineData("2.0.0", "1.9.9", false)]
    [InlineData("1.0.0", "1.0.1-beta", true)]
    [InlineData("1.0.0", "1.0.0+build123", false)]
    [InlineData("1.3.0", "1.4.0", true)]
    [InlineData("1.3.0", "v1.4.0", true)]
    public void UpdateInfo_HasUpdate_ComparesVersionsCorrectly(string current, string next, bool expected)
    {
        var info = new UpdateInfo(
            CurrentVersion: current,
            NewVersion: next,
            ReleaseTitle: "Test Release",
            ReleaseNotes: "Notes",
            DownloadUrl: "https://example.com/setup.exe",
            FileSizeBytes: 1024,
            Sha256Checksum: null,
            PublishedAt: DateTimeOffset.UtcNow);

        info.HasUpdate.Should().Be(expected);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ValidGitHubReleaseJson_ParsesUpdateInfoCorrectly()
    {
        // Arrange
        var fakeJson = """
        {
          "tag_name": "v1.2.0",
          "name": "Lexora v1.2.0 Spring Edition",
          "body": "### New Features\n* Auto update support\n\nSHA256: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
          "published_at": "2026-10-17T00:00:00Z",
          "assets": [
            {
              "name": "Lexora-Setup-v1.2.0.exe",
              "browser_download_url": "https://github.com/Nguyen-Trung-Tien/Lexora/releases/download/v1.2.0/Lexora-Setup-v1.2.0.exe",
              "size": 52428800
            }
          ]
        }
        """;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fakeJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var mockBackup = new Mock<IDatabaseBackupService>();
        var mockLogger = new Mock<ILogger<UpdateService>>();

        var service = new UpdateService(mockBackup.Object, mockLogger.Object, httpClient);

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        result.Should().NotBeNull();
        result!.NewVersion.Should().Be("v1.2.0");
        result.ReleaseTitle.Should().Be("Lexora v1.2.0 Spring Edition");
        result.DownloadUrl.Should().Be("https://github.com/Nguyen-Trung-Tien/Lexora/releases/download/v1.2.0/Lexora-Setup-v1.2.0.exe");
        result.FileSizeBytes.Should().Be(52428800);
        result.Sha256Checksum.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Fact]
    public async Task DownloadUpdateAsync_WhenChecksumMatches_DownloadsFileSuccessfully()
    {
        // Arrange
        var contentBytes = Encoding.UTF8.GetBytes("Fake Inno Setup Installer Executable Content");
        var hashBytes = SHA256.HashData(contentBytes);
        var validHashHex = Convert.ToHexString(hashBytes);

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(contentBytes)
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var mockBackup = new Mock<IDatabaseBackupService>();
        var mockLogger = new Mock<ILogger<UpdateService>>();

        var service = new UpdateService(mockBackup.Object, mockLogger.Object, httpClient);

        var updateInfo = new UpdateInfo(
            CurrentVersion: "1.0.0",
            NewVersion: "v1.2.0",
            ReleaseTitle: "Test",
            ReleaseNotes: "Notes",
            DownloadUrl: "https://example.com/Lexora-Setup-v1.2.0.exe",
            FileSizeBytes: contentBytes.Length,
            Sha256Checksum: validHashHex,
            PublishedAt: DateTimeOffset.UtcNow);

        var progressCalls = 0;
        var progress = new Progress<UpdateDownloadProgress>(p => progressCalls++);

        // Act
        var downloadedPath = await service.DownloadUpdateAsync(updateInfo, progress);

        // Assert
        File.Exists(downloadedPath).Should().BeTrue();
        var downloadedContent = await File.ReadAllBytesAsync(downloadedPath);
        downloadedContent.Should().Equal(contentBytes);

        // Cleanup
        try { File.Delete(downloadedPath); } catch { /* ignore */ }
    }

    [Fact]
    public async Task DownloadUpdateAsync_WhenChecksumMismatches_ThrowsInvalidDataExceptionAndDeletesFile()
    {
        // Arrange
        var contentBytes = Encoding.UTF8.GetBytes("Corrupted Or Tampered Payload Content");
        var wrongHashHex = "0000000000000000000000000000000000000000000000000000000000000000";

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(contentBytes)
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var mockBackup = new Mock<IDatabaseBackupService>();
        var mockLogger = new Mock<ILogger<UpdateService>>();

        var service = new UpdateService(mockBackup.Object, mockLogger.Object, httpClient);

        var updateInfo = new UpdateInfo(
            CurrentVersion: "1.0.0",
            NewVersion: "v1.2.0",
            ReleaseTitle: "Tampered Test",
            ReleaseNotes: "Notes",
            DownloadUrl: "https://example.com/Lexora-Setup-v1.2.0.exe",
            FileSizeBytes: contentBytes.Length,
            Sha256Checksum: wrongHashHex,
            PublishedAt: DateTimeOffset.UtcNow);

        // Act
        Func<Task> act = async () => await service.DownloadUpdateAsync(updateInfo);

        // Assert
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*SHA-256 checksum mismatch*");
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenSuccessful_PopulatesLatestDiscoveredUpdate()
    {
        // Arrange
        var fakeJson = """
        {
          "tag_name": "v1.3.0",
          "name": "Lexora v1.3.0 Autumn",
          "body": "Bugfixes and notifications",
          "published_at": "2026-10-17T00:00:00Z",
          "assets": [
            {
              "name": "Lexora-Setup-v1.3.0.exe",
              "browser_download_url": "https://github.com/Nguyen-Trung-Tien/Lexora/releases/download/v1.3.0/Lexora-Setup-v1.3.0.exe",
              "size": 1024
            }
          ]
        }
        """;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fakeJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var mockBackup = new Mock<IDatabaseBackupService>();
        var mockLogger = new Mock<ILogger<UpdateService>>();

        var service = new UpdateService(mockBackup.Object, mockLogger.Object, httpClient);
        service.LatestDiscoveredUpdate.Should().BeNull();

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        result.Should().NotBeNull();
        service.LatestDiscoveredUpdate.Should().NotBeNull();
        service.LatestDiscoveredUpdate.Should().BeSameAs(result);
        service.LatestDiscoveredUpdate!.NewVersion.Should().Be("v1.3.0");
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenDigestPresentInAsset_ParsesSha256Successfully()
    {
        // Arrange
        var fakeJson = """
        {
          "tag_name": "v1.4.0",
          "name": "Lexora v1.4.0 Final",
          "body": "Major UI streamlining and performance improvements",
          "published_at": "2026-10-28T00:00:00Z",
          "assets": [
            {
              "name": "Lexora-Setup-v1.4.0.exe",
              "browser_download_url": "https://github.com/Nguyen-Trung-Tien/Lexora/releases/download/v1.4.0/Lexora-Setup-v1.4.0.exe",
              "size": 67212867,
              "digest": "sha256:b794c417745828338f68f51d9e0b20f0931e3ee34d8456694823784d448c614d"
            }
          ]
        }
        """;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fakeJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var mockBackup = new Mock<IDatabaseBackupService>();
        var mockLogger = new Mock<ILogger<UpdateService>>();

        var service = new UpdateService(mockBackup.Object, mockLogger.Object, httpClient);

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        result.Should().NotBeNull();
        result!.NewVersion.Should().Be("v1.4.0");
        result.Sha256Checksum.Should().Be("b794c417745828338f68f51d9e0b20f0931e3ee34d8456694823784d448c614d");
        result.DownloadUrl.Should().Be("https://github.com/Nguyen-Trung-Tien/Lexora/releases/download/v1.4.0/Lexora-Setup-v1.4.0.exe");
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenLatestReturns404_FallsBackToReleasesList()
    {
        // Arrange
        var fakeListJson = """
        [
          {
            "tag_name": "v1.4.0",
            "name": "Lexora v1.4.0",
            "body": "Fallback release notes",
            "draft": false,
            "published_at": "2026-10-28T00:00:00Z",
            "assets": [
              {
                "name": "Lexora-Setup-v1.4.0.exe",
                "browser_download_url": "https://github.com/Nguyen-Trung-Tien/Lexora/releases/download/v1.4.0/Lexora-Setup-v1.4.0.exe",
                "size": 67212867
              }
            ]
          }
        ]
        """;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/releases/latest")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().EndsWith("/releases")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fakeListJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var mockBackup = new Mock<IDatabaseBackupService>();
        var mockLogger = new Mock<ILogger<UpdateService>>();

        var service = new UpdateService(mockBackup.Object, mockLogger.Object, httpClient);

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        result.Should().NotBeNull();
        result!.NewVersion.Should().Be("v1.4.0");
        result.DownloadUrl.Should().Be("https://github.com/Nguyen-Trung-Tien/Lexora/releases/download/v1.4.0/Lexora-Setup-v1.4.0.exe");
    }

    [Fact]
    public void UserPreferences_AutoCheckUpdates_DefaultsToTrue()
    {
        var prefs = new UserPreferences();
        prefs.AutoCheckUpdates.Should().BeTrue();
    }
}
