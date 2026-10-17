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

public class AdaptiveQuizServiceTests
{
    private readonly Mock<IVocabularyRepository> _mockVocabRepo;
    private readonly Mock<IReviewRepository> _mockReviewRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly AdaptiveQuizService _adaptiveService;

    public AdaptiveQuizServiceTests()
    {
        _mockVocabRepo = new Mock<IVocabularyRepository>();
        _mockReviewRepo = new Mock<IReviewRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _adaptiveService = new AdaptiveQuizService(
            _mockVocabRepo.Object,
            _mockReviewRepo.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task StartQuizAsync_WhenWordsAvailable_InitializesAdaptiveSession()
    {
        // Arrange
        var testWords = new List<Word>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Term = "diligent",
                Level = CefrLevel.B1,
                Meanings = new List<WordMeaning>
                {
                    new() { DefinitionVi = "chăm chỉ, cần cù", DefinitionEn = "hardworking" }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Term = "meticulous",
                Level = CefrLevel.C1,
                Meanings = new List<WordMeaning>
                {
                    new() { DefinitionVi = "tỉ mỉ, cẩn thận", DefinitionEn = "precise" }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Term = "resilient",
                Level = CefrLevel.B2,
                Meanings = new List<WordMeaning>
                {
                    new() { DefinitionVi = "kiên cường", DefinitionEn = "tough" }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Term = "innovative",
                Level = CefrLevel.B2,
                Meanings = new List<WordMeaning>
                {
                    new() { DefinitionVi = "sáng tạo", DefinitionEn = "creative" }
                }
            }
        };

        _mockVocabRepo.Setup(r => r.SearchAsync(null, null, null, null, null, null, 0, 300, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testWords);

        var config = new AdaptiveQuizConfigDto(
            QuestionCount: 5,
            InitialLevel: CefrLevel.B1,
            FocusWeakWords: false
        );

        // Act
        var firstQuestion = await _adaptiveService.StartQuizAsync(config);

        // Assert
        firstQuestion.Should().NotBeNull();
        firstQuestion!.QuestionIndex.Should().Be(1);
        firstQuestion.DifficultyLevel.Should().Be(CefrLevel.B1);
        firstQuestion.Options.Should().NotBeEmpty();
        _adaptiveService.HasActiveSession(firstQuestion.SessionId).Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAnswerAndFinishQuizAsync_CalculatesFinalReadinessAndBandProjections()
    {
        // Arrange
        var word = new Word
        {
            Id = Guid.NewGuid(),
            Term = "profound",
            Level = CefrLevel.B2,
            Meanings = new List<WordMeaning>
            {
                new() { DefinitionVi = "sâu sắc", DefinitionEn = "deep" }
            }
        };

        _mockVocabRepo.Setup(r => r.SearchAsync(null, null, null, null, null, null, 0, 300, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { word });

        var config = new AdaptiveQuizConfigDto(
            QuestionCount: 2,
            InitialLevel: CefrLevel.B1,
            FocusWeakWords: false
        );

        var question = await _adaptiveService.StartQuizAsync(config);
        question.Should().NotBeNull();

        // Act: Submit answer to question 1
        var eval = await _adaptiveService.SubmitAnswerAsync(
            question!.SessionId,
            questionIndex: 1,
            selectedOptionIndex: question.CorrectOptionIndex,
            responseTimeMs: 2500);

        eval.Should().NotBeNull();
        eval.IsCorrect.Should().BeTrue();

        // Finish quiz
        var summary = await _adaptiveService.FinishQuizAsync(question.SessionId);

        // Assert
        summary.Should().NotBeNull();
        summary.TotalQuestions.Should().BeGreaterThan(0);
        summary.CorrectCount.Should().Be(1);
        summary.EstimatedToeicBand.Should().NotBeNullOrWhiteSpace();
        summary.EstimatedIeltsBand.Should().NotBeNullOrWhiteSpace();
        summary.EstimatedVstepLevel.Should().NotBeNullOrWhiteSpace();
    }
}
