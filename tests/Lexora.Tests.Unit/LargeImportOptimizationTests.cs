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

public class LargeImportOptimizationTests
{
    [Fact]
    public void ProgressThrottler_WhenCalledRapidlyWithSamePercent_ShouldThrottleProgressReports()
    {
        // Arrange
        int reportCount = 0;
        var progressMock = new Mock<IProgress<ProgressReport>>();
        progressMock.Setup(p => p.Report(It.IsAny<ProgressReport>()))
            .Callback<ProgressReport>(_ => reportCount++);

        var throttler = new ProgressThrottler(progressMock.Object, throttleMs: 100);

        // Act - Call 100 times in immediate loop with same percent and no force
        for (int i = 0; i < 100; i++)
        {
            throttler.Report(percent: 50, current: i, total: 100, message: $"Item {i}");
        }

        // Assert - First call is immediate, subsequent rapid calls within 100ms are throttled
        reportCount.Should().BeLessThan(5);
    }

    [Fact]
    public void ProgressThrottler_WhenForced_ShouldReportImmediately()
    {
        // Arrange
        int reportCount = 0;
        var progressMock = new Mock<IProgress<ProgressReport>>();
        progressMock.Setup(p => p.Report(It.IsAny<ProgressReport>()))
            .Callback<ProgressReport>(_ => reportCount++);

        var throttler = new ProgressThrottler(progressMock.Object, throttleMs: 500);

        // Act - Call 5 times with force: true
        for (int i = 0; i < 5; i++)
        {
            throttler.Report(percent: 10, current: i, total: 5, message: $"Step {i}", force: true);
        }

        // Assert
        reportCount.Should().Be(5);
    }

    [Fact]
    public async Task GrammarService_GetPagedRulesAsync_ShouldReturnCorrectPageAndTotal()
    {
        // Arrange
        var repoMock = new Mock<IGrammarRepository>();
        var uowMock = new Mock<IUnitOfWork>();

        var testRules = new List<GrammarRule>();
        for (int i = 1; i <= 25; i++)
        {
            testRules.Add(new GrammarRule
            {
                Id = Guid.NewGuid(),
                Title = $"Rule {i:D2}",
                Structure = $"S + V + {i}",
                ExplanationVi = $"Giải thích quy tắc {i}",
                Level = CefrLevel.B1,
                ExamTarget = ExamType.Ielts,
                Topic = "Tenses"
            });
        }

        repoMock.Setup(r => r.CountFilteredAsync(
                It.IsAny<CefrLevel?>(),
                It.IsAny<ExamType?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        repoMock.Setup(r => r.GetFilteredPagedAsync(
                0,
                10,
                It.IsAny<CefrLevel?>(),
                It.IsAny<ExamType?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(testRules.Take(10).ToList());

        var service = new GrammarService(repoMock.Object, uowMock.Object);

        // Act
        var result = await service.GetPagedRulesAsync(page: 1, pageSize: 10);

        // Assert
        result.TotalCount.Should().Be(25);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Rules.Should().HaveCount(10);
        result.Rules[0].Title.Should().Be("Rule 01");
    }

    [Fact]
    public async Task ExportImportService_CommitSmartImportAsync_WhenCancelled_ShouldRollbackAndThrow()
    {
        // Arrange
        var vocabRepoMock = new Mock<IVocabularyRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var service = new ExportImportService(vocabRepoMock.Object, uowMock.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled token

        var items = new List<SmartImportItemDto>
        {
            new(
                RowIndex: 1,
                Term: "resilience",
                Level: CefrLevel.C1,
                PartOfSpeech: PartOfSpeech.Noun,
                DefinitionVi: "Khả năng phục hồi",
                DefinitionEn: "Ability to recover",
                PhoneticUk: null,
                PhoneticUs: null,
                Examples: null,
                Topics: null,
                ExamTags: null,
                Notes: null,
                IsDuplicate: false,
                IsValid: true,
                ValidationMessage: null
            )
        };

        var request = new SmartImportCommitRequestDto(items, ImportConflictResolution.Skip);

        // Act
        Func<Task> act = async () => await service.CommitSmartImportAsync(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        uowMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
