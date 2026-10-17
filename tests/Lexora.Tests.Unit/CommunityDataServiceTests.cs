using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class CommunityDataServiceTests
{
    private readonly Mock<IExportImportService> _exportImportServiceMock;
    private readonly Mock<IGrammarExportImportService> _grammarExportImportServiceMock;
    private readonly Mock<IUserPreferencesService> _userPreferencesServiceMock;
    private readonly Mock<ILogger<CommunityDataService>> _loggerMock;

    public CommunityDataServiceTests()
    {
        _exportImportServiceMock = new Mock<IExportImportService>();
        _grammarExportImportServiceMock = new Mock<IGrammarExportImportService>();
        _userPreferencesServiceMock = new Mock<IUserPreferencesService>();
        _loggerMock = new Mock<ILogger<CommunityDataService>>();

        _userPreferencesServiceMock.Setup(u => u.GetPreferences())
            .Returns(new UserPreferences());
    }

    [Fact]
    public async Task GetCatalogAsync_ReturnsFallbackCatalog_WhenRemoteIsUnreachable()
    {
        // Arrange: Custom HttpClient that returns 404 (simulating private repo unauthenticated)
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "Not Found");
        using var httpClient = new HttpClient(handler);

        var service = new CommunityDataService(
            _exportImportServiceMock.Object,
            _grammarExportImportServiceMock.Object,
            _userPreferencesServiceMock.Object,
            _loggerMock.Object,
            httpClient);

        // Act
        var catalog = await service.GetCatalogAsync();

        // Assert
        catalog.Should().NotBeNull();
        catalog.Packages.Should().NotBeEmpty();
        catalog.Packages.Should().Contain(p => p.Category == "Vocabulary");
        catalog.Packages.Should().Contain(p => p.Category == "Grammar");
    }

    [Fact]
    public async Task GetAvailablePackagesAsync_WithFilter_FiltersCorrectly()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "Not Found");
        using var httpClient = new HttpClient(handler);

        var service = new CommunityDataService(
            _exportImportServiceMock.Object,
            _grammarExportImportServiceMock.Object,
            _userPreferencesServiceMock.Object,
            _loggerMock.Object,
            httpClient);

        var vocabFilter = new CommunityPackageFilterDto(Category: "Vocabulary");
        var grammarFilter = new CommunityPackageFilterDto(Category: "Grammar");

        // Act
        var vocabPackages = await service.GetAvailablePackagesAsync(vocabFilter);
        var grammarPackages = await service.GetAvailablePackagesAsync(grammarFilter);

        // Assert
        vocabPackages.Should().NotBeEmpty();
        vocabPackages.Should().OnlyContain(p => p.Category == "Vocabulary");

        grammarPackages.Should().NotBeEmpty();
        grammarPackages.Should().OnlyContain(p => p.Category == "Grammar");
    }

    [Fact]
    public async Task InstallPackageAsync_VocabularyPackage_DelegatesToExportImportService()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(handler);

        var service = new CommunityDataService(
            _exportImportServiceMock.Object,
            _grammarExportImportServiceMock.Object,
            _userPreferencesServiceMock.Object,
            _loggerMock.Object,
            httpClient);

        var samplePayload = new CommunityPackagePayloadDto
        {
            Metadata = new CommunityPackageMetadataDto
            {
                Id = "test-vocab-pkg",
                Name = "Test Vocab",
                Category = "Vocabulary",
                ItemCount = 1
            },
            VocabularyWords = new List<WordExportModel>
            {
                new() { Term = "persist", PhoneticUk = "/pəˈsɪst/", Level = CefrLevel.B2 }
            }
        };

        _exportImportServiceMock.Setup(e => e.ImportFromJsonAsync(
            It.IsAny<string>(),
            ImportConflictResolution.Skip,
            It.IsAny<CancellationToken>(),
            It.IsAny<IProgress<ProgressReport>>()))
            .ReturnsAsync(new ImportResult(TotalImported: 1, TotalSkipped: 0, Errors: [], TotalUpdated: 0));

        // Act
        var result = await service.InstallPackageAsync(samplePayload, ImportConflictResolution.Skip);

        // Assert
        result.TotalImported.Should().Be(1);
        _exportImportServiceMock.Verify(e => e.ImportFromJsonAsync(
            It.IsAny<string>(),
            ImportConflictResolution.Skip,
            It.IsAny<CancellationToken>(),
            It.IsAny<IProgress<ProgressReport>>()), Times.Once);
    }

    [Fact]
    public async Task InstallPackageAsync_GrammarPackage_DelegatesToGrammarExportImportService()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(handler);

        var service = new CommunityDataService(
            _exportImportServiceMock.Object,
            _grammarExportImportServiceMock.Object,
            _userPreferencesServiceMock.Object,
            _loggerMock.Object,
            httpClient);

        var samplePayload = new CommunityPackagePayloadDto
        {
            Metadata = new CommunityPackageMetadataDto
            {
                Id = "test-grammar-pkg",
                Name = "Test Grammar",
                Category = "Grammar",
                ItemCount = 1
            },
            GrammarRules = new List<GrammarRuleExportModel>
            {
                new() { Title = "Present Simple", Structure = "S + V(s/es)" }
            }
        };

        _grammarExportImportServiceMock.Setup(g => g.ImportFromJsonAsync(
            It.IsAny<string>(),
            ImportConflictResolution.Overwrite,
            It.IsAny<CancellationToken>(),
            It.IsAny<IProgress<ProgressReport>>()))
            .ReturnsAsync(new ImportResult(TotalImported: 1, TotalSkipped: 0, Errors: [], TotalUpdated: 0));

        // Act
        var result = await service.InstallPackageAsync(samplePayload, ImportConflictResolution.Overwrite);

        // Assert
        result.TotalImported.Should().Be(1);
        _grammarExportImportServiceMock.Verify(g => g.ImportFromJsonAsync(
            It.IsAny<string>(),
            ImportConflictResolution.Overwrite,
            It.IsAny<CancellationToken>(),
            It.IsAny<IProgress<ProgressReport>>()), Times.Once);
    }

    [Fact]
    public async Task ExportPackageToJsonAsync_Vocabulary_CreatesValidCommunityPackage()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(handler);

        var service = new CommunityDataService(
            _exportImportServiceMock.Object,
            _grammarExportImportServiceMock.Object,
            _userPreferencesServiceMock.Object,
            _loggerMock.Object,
            httpClient);

        var wordsJson = JsonSerializer.Serialize(new List<WordExportModel>
        {
            new() { Term = "diligent", Level = CefrLevel.B2 }
        });

        _exportImportServiceMock.Setup(e => e.ExportToJsonAsync(
            It.IsAny<IEnumerable<Guid>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<IProgress<ProgressReport>>()))
            .ReturnsAsync(wordsJson);

        var metadata = new CommunityPackageMetadataDto
        {
            Name = "My Vocabulary Set",
            Category = "Vocabulary",
            Level = "B2",
            Author = "Community User",
            Description = "A comprehensive set",
            Tags = new List<string> { "b2", "academic" }
        };

        // Act
        var packageJson = await service.ExportPackageToJsonAsync(metadata);

        // Assert
        packageJson.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(packageJson);
        var root = doc.RootElement;

        root.GetProperty("metadata").GetProperty("name").GetString().Should().Be("My Vocabulary Set");
        root.GetProperty("metadata").GetProperty("category").GetString().Should().Be("Vocabulary");
        root.GetProperty("metadata").GetProperty("author").GetString().Should().Be("Community User");
        root.GetProperty("metadata").GetProperty("itemCount").GetInt32().Should().Be(1);
        root.GetProperty("vocabularyWords").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task DownloadPackageAsync_InvalidChecksum_ThrowsInvalidDataException()
    {
        // Arrange: Payload JSON with wrong hash expectation
        var rawJson = JsonSerializer.Serialize(new CommunityPackagePayloadDto
        {
            Metadata = new CommunityPackageMetadataDto
            {
                Id = "tampered-pkg",
                Name = "Tampered Package",
                Category = "Vocabulary"
            },
            VocabularyWords = new List<WordExportModel> { new() { Term = "tampered", Level = CefrLevel.B2 } }
        });

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, rawJson);
        using var httpClient = new HttpClient(handler);

        var service = new CommunityDataService(
            _exportImportServiceMock.Object,
            _grammarExportImportServiceMock.Object,
            _userPreferencesServiceMock.Object,
            _loggerMock.Object,
            httpClient);

        var pkgMetadata = new CommunityPackageMetadataDto
        {
            Id = "tampered-pkg",
            Name = "Tampered Package",
            DownloadUrl = "https://example.com/tampered.json",
            ChecksumSha256 = "0000000000000000000000000000000000000000000000000000000000000000" // fake hash
        };

        // Act & Assert
        var act = async () => await service.DownloadPackageAsync(pkgMetadata);
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*toàn vẹn*");
    }

    [Fact]
    public async Task InstalledRecordTracking_CanSaveAndRemoveRecords()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(handler);

        var service = new CommunityDataService(
            _exportImportServiceMock.Object,
            _grammarExportImportServiceMock.Object,
            _userPreferencesServiceMock.Object,
            _loggerMock.Object,
            httpClient);

        var pkgId = "tracking-test-" + Guid.NewGuid().ToString("N");
        var payload = new CommunityPackagePayloadDto
        {
            Metadata = new CommunityPackageMetadataDto
            {
                Id = pkgId,
                Name = "Tracking Test Package",
                Category = "Vocabulary",
                Version = "1.0.0",
                ItemCount = 1
            },
            VocabularyWords = new List<WordExportModel>
            {
                new() { Term = "verify", Level = CefrLevel.B1 }
            }
        };

        _exportImportServiceMock.Setup(e => e.ImportFromJsonAsync(
            It.IsAny<string>(),
            It.IsAny<ImportConflictResolution>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<IProgress<ProgressReport>>()))
            .ReturnsAsync(new ImportResult(1, 0, [], 0));

        // Act: Install package to track
        await service.InstallPackageAsync(payload);
        var installed = await service.GetInstalledPackagesAsync();
        installed.Should().Contain(r => r.PackageId == pkgId);

        // Act: Delete record
        var deleted = await service.DeleteInstalledRecordAsync(pkgId);
        deleted.Should().BeTrue();

        var remaining = await service.GetInstalledPackagesAsync();
        remaining.Should().NotContain(r => r.PackageId == pkgId);
    }

    [Fact]
    public async Task ParseAndValidatePackageAsync_EmptyOrInvalidJson_ThrowsInvalidDataException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(handler);

        var service = new CommunityDataService(
            _exportImportServiceMock.Object,
            _grammarExportImportServiceMock.Object,
            _userPreferencesServiceMock.Object,
            _loggerMock.Object,
            httpClient);

        // Act & Assert
        var emptyAct = async () => await service.ParseAndValidatePackageAsync("");
        await emptyAct.Should().ThrowAsync<InvalidDataException>();

        var invalidJsonAct = async () => await service.ParseAndValidatePackageAsync("{ invalid json }");
        await invalidJsonAct.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task ParseAndValidatePackageAsync_MissingMetadata_ThrowsInvalidDataException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(handler);

        var service = new CommunityDataService(
            _exportImportServiceMock.Object,
            _grammarExportImportServiceMock.Object,
            _userPreferencesServiceMock.Object,
            _loggerMock.Object,
            httpClient);

        var noIdJson = JsonSerializer.Serialize(new CommunityPackagePayloadDto
        {
            Metadata = new CommunityPackageMetadataDto { Id = "", Name = "No Id" }
        });

        // Act & Assert
        var act = async () => await service.ParseAndValidatePackageAsync(noIdJson);
        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*Id*");
    }

    [Fact]
    public async Task ImportPackageFromFileOrUrlAsync_InsecureHttpUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(handler);

        var service = new CommunityDataService(
            _exportImportServiceMock.Object,
            _grammarExportImportServiceMock.Object,
            _userPreferencesServiceMock.Object,
            _loggerMock.Object,
            httpClient);

        // Act & Assert: http (insecure) must be rejected per security specification
        var act = async () => await service.ImportPackageFromFileOrUrlAsync("http://insecure-domain.com/pkg.json");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*HTTPS*");
    }

    [Fact]
    public async Task ImportPackageFromFileOrUrlAsync_DirectJsonString_ParsesAndInstalls()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(handler);

        var service = new CommunityDataService(
            _exportImportServiceMock.Object,
            _grammarExportImportServiceMock.Object,
            _userPreferencesServiceMock.Object,
            _loggerMock.Object,
            httpClient);

        var jsonContent = JsonSerializer.Serialize(new CommunityPackagePayloadDto
        {
            Metadata = new CommunityPackageMetadataDto
            {
                Id = "direct-json-pkg",
                Name = "Direct JSON Package",
                Category = "Vocabulary",
                ItemCount = 1
            },
            VocabularyWords = new List<WordExportModel>
            {
                new() { Term = "autonomous", Level = CefrLevel.C1 }
            }
        });

        _exportImportServiceMock.Setup(e => e.ImportFromJsonAsync(
            It.IsAny<string>(),
            It.IsAny<ImportConflictResolution>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<IProgress<ProgressReport>>()))
            .ReturnsAsync(new ImportResult(1, 0, [], 0));

        // Act
        var result = await service.ImportPackageFromFileOrUrlAsync(jsonContent, ImportConflictResolution.Overwrite);

        // Assert
        result.TotalImported.Should().Be(1);
    }
}

internal class MockHttpMessageHandler(HttpStatusCode statusCode, string responseContent) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
