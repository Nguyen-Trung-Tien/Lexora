using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class HomophoneServiceAndExportImportTests
{
    private readonly Mock<IHomophoneRepository> _homophoneRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly HomophoneService _homophoneService;
    private readonly HomophoneExportImportService _exportImportService;

    public HomophoneServiceAndExportImportTests()
    {
        _homophoneService = new HomophoneService(_homophoneRepoMock.Object, _unitOfWorkMock.Object);
        _exportImportService = new HomophoneExportImportService(_homophoneRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateGroupAsync_ShouldAddGroupAndSave_WhenValidDtoProvided()
    {
        // Arrange
        var dto = new CreateHomophoneGroupDto
        {
            Title = "bare / bear",
            PhoneticIpa = "/beə(r)/",
            Type = HomophoneType.ExactHomophone,
            DifficultyLevel = CefrLevel.B1,
            ExamTag = ExamType.Toeic,
            DifferentiationTip = "Bare là trần trụi; Bear là chịu đựng",
            Items =
            [
                new CreateHomophoneItemDto
                {
                    Term = "bare",
                    PartOfSpeech = PartOfSpeech.Adjective,
                    MeaningVi = "trần trụi"
                },
                new CreateHomophoneItemDto
                {
                    Term = "bear",
                    PartOfSpeech = PartOfSpeech.Verb,
                    MeaningVi = "chịu đựng"
                }
            ]
        };

        // Act
        var result = await _homophoneService.CreateGroupAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("bare / bear");
        result.Items.Should().HaveCount(2);
        _homophoneRepoMock.Verify(r => r.AddAsync(It.IsAny<HomophoneGroup>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGroupAsync_ShouldReturnTrue_WhenGroupExists()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var existingGroup = new HomophoneGroup
        {
            Id = groupId,
            Title = "peace / piece"
        };

        _homophoneRepoMock.Setup(r => r.GetWithItemsAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        // Act
        var result = await _homophoneService.DeleteGroupAsync(groupId);

        // Assert
        result.Should().BeTrue();
        _homophoneRepoMock.Verify(r => r.DeleteAsync(existingGroup, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportToJsonAsync_ShouldProduceValidJsonString()
    {
        // Arrange
        var sampleGroups = new List<HomophoneGroup>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "knight / night",
                PhoneticIpa = "/naɪt/",
                Type = HomophoneType.ExactHomophone,
                Items =
                {
                    new HomophoneItem { Term = "knight", MeaningVi = "hiệp sĩ" },
                    new HomophoneItem { Term = "night", MeaningVi = "ban đêm" }
                }
            }
        };

        _homophoneRepoMock.Setup(r => r.GetAllWithItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleGroups);

        // Act
        var json = await _exportImportService.ExportToJsonAsync();

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("knight / night");
        json.Should().Contain("hiệp sĩ");
    }

    [Fact]
    public async Task AnalyzeImportAsync_ShouldAutoDetectJson_AndMarkDuplicates()
    {
        // Arrange
        var json = """
            [
              {
                "title": "bare / bear",
                "phoneticIpa": "/beə(r)/",
                "items": [
                  { "term": "bare", "meaningVi": "trần trụi" },
                  { "term": "bear", "meaningVi": "chịu đựng" }
                ]
              }
            ]
            """;

        _homophoneRepoMock.Setup(r => r.GetAllGroupTitlesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bare / bear" });

        // Act
        var analysis = await _exportImportService.AnalyzeImportAsync(json, "data.json");

        // Assert
        analysis.DetectedFormat.Should().Be(DataFormat.Json);
        analysis.TotalGroupsFound.Should().Be(1);
        analysis.DuplicateGroupsCount.Should().Be(1);
        analysis.PreviewItems.First().IsDuplicate.Should().BeTrue();
    }

    [Fact]
    public async Task CommitImportAsync_WithSkipConflict_ShouldSkipDuplicates()
    {
        // Arrange
        var request = new SmartHomophoneCommitRequestDto
        {
            ConflictResolution = ImportConflictResolution.Skip,
            GroupsToCommit =
            [
                new HomophoneImportExportGroupDto
                {
                    Title = "bare / bear",
                    Items = [new HomophoneImportExportItemDto { Term = "bare", MeaningVi = "trần trụi" }]
                }
            ]
        };

        var existingGroup = new HomophoneGroup { Title = "bare / bear" };
        var dict = new Dictionary<string, HomophoneGroup>(StringComparer.OrdinalIgnoreCase)
        {
            ["bare / bear"] = existingGroup
        };

        _homophoneRepoMock.Setup(r => r.GetGroupsByTitlesWithItemsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dict);

        // Act
        var result = await _exportImportService.CommitImportAsync(request);

        // Assert
        result.TotalImported.Should().Be(0);
        result.TotalSkipped.Should().Be(1);
        _homophoneRepoMock.Verify(r => r.AddAsync(It.IsAny<HomophoneGroup>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GetSampleTemplate_ShouldReturnTemplates_ForJsonCsvTxt()
    {
        // Act & Assert
        _exportImportService.GetSampleTemplate("json").Should().Contain("bare / bear");
        _exportImportService.GetSampleTemplate("csv").Should().Contain("GroupTitle,Phonetic");
        _exportImportService.GetSampleTemplate("txt").Should().Contain("bare / bear [/beə(r)/]");
    }
}
