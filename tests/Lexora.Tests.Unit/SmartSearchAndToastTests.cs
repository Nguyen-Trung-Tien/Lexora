using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Lexora.Presentation.Services;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class SmartSearchAndToastTests
{
    private readonly Mock<ILocalizationService> _mockLoc;

    public SmartSearchAndToastTests()
    {
        _mockLoc = new Mock<ILocalizationService>();
        _mockLoc.Setup(l => l.GetString("Toast_SuccessTitle")).Returns("Success");
        _mockLoc.Setup(l => l.GetString("Toast_ErrorTitle")).Returns("Error");
        _mockLoc.Setup(l => l.GetString("Toast_WarningTitle")).Returns("Warning");
        _mockLoc.Setup(l => l.GetString("Toast_InfoTitle")).Returns("Info");
    }

    [Fact]
    public void ToastNotificationService_WhenShowingIdenticalMessageRapidly_ShouldDeduplicate()
    {
        // Arrange
        var toastService = new ToastNotificationService(_mockLoc.Object);
        int eventCount = 0;
        toastService.ToastRequested += (n) => eventCount++;

        // Act - rapidly trigger 3 identical toasts
        toastService.ShowSuccess("Operation succeeded", "Success");
        toastService.ShowSuccess("Operation succeeded", "Success");
        toastService.ShowSuccess("Operation succeeded", "Success");

        // Assert - only the first one should fire
        eventCount.Should().Be(1);
    }

    [Fact]
    public void ToastNotificationService_WhenShowingDifferentMessages_ShouldAllowAll()
    {
        // Arrange
        var toastService = new ToastNotificationService(_mockLoc.Object);
        int eventCount = 0;
        toastService.ToastRequested += (n) => eventCount++;

        // Act
        toastService.ShowSuccess("First operation succeeded", "Success");
        toastService.ShowWarning("Second operation had a warning", "Warning");
        toastService.ShowError("Third operation failed", "Error");

        // Assert
        eventCount.Should().Be(3);
    }

    [Fact]
    public void ToastNotificationService_WithEmptyTitle_ShouldSupplyDefaultTitle()
    {
        // Arrange
        var toastService = new ToastNotificationService(_mockLoc.Object);
        ToastNotification? received = null;
        toastService.ToastRequested += (n) => received = n;

        // Act
        toastService.Show(ToastType.Error, "Database lock acquired", string.Empty);

        // Assert
        received.Should().NotBeNull();
        received!.Title.Should().Be("Error");
        received.Type.Should().Be(ToastType.Error);
        received.Message.Should().Be("Database lock acquired");
    }

    [Fact]
    public async Task VocabularyService_DeleteWordsAsync_WhenExceptionOccurs_ShouldRollback()
    {
        // Arrange
        var mockVocabRepo = new Mock<IVocabularyRepository>();
        var mockReviewRepo = new Mock<IReviewRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();

        var wordId = Guid.NewGuid();
        var word = new Word { Id = wordId, Term = "rollback_test" };

        mockVocabRepo.Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { word });

        mockVocabRepo.Setup(r => r.DeleteAsync(word, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated SQLite write error"));

        var service = new VocabularyService(mockVocabRepo.Object, mockReviewRepo.Object, mockUnitOfWork.Object);

        // Act
        var act = async () => await service.DeleteWordsAsync([wordId]);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated SQLite write error");

        mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GrammarService_DeleteRulesAsync_WhenExceptionOccurs_ShouldRollback()
    {
        // Arrange
        var mockGrammarRepo = new Mock<IGrammarRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();

        var ruleId = Guid.NewGuid();
        var rule = new GrammarRule { Id = ruleId, Title = "Past Perfect" };

        mockGrammarRepo.Setup(r => r.GetWithExamplesAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        mockGrammarRepo.Setup(r => r.DeleteAsync(rule, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Disk I/O error"));

        var service = new GrammarService(mockGrammarRepo.Object, mockUnitOfWork.Object);

        // Act
        var act = async () => await service.DeleteRulesAsync([ruleId]);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Disk I/O error");

        mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
