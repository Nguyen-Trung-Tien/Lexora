using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class StudyPlannerServiceTests : IDisposable
{
    private readonly Mock<IReviewRepository> _mockReviewRepo;
    private readonly Mock<IVocabularyRepository> _mockVocabRepo;
    private readonly Mock<IGrammarRepository> _mockGrammarRepo;
    private readonly string _tempStoragePath;
    private readonly StudyPlannerService _plannerService;

    public StudyPlannerServiceTests()
    {
        _mockReviewRepo = new Mock<IReviewRepository>();
        _mockVocabRepo = new Mock<IVocabularyRepository>();
        _mockGrammarRepo = new Mock<IGrammarRepository>();
        _tempStoragePath = Path.Combine(Path.GetTempPath(), $"planner_test_{Guid.NewGuid()}.json");

        _plannerService = new StudyPlannerService(
            _mockReviewRepo.Object,
            _mockVocabRepo.Object,
            _mockGrammarRepo.Object,
            _tempStoragePath);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempStoragePath))
            {
                File.Delete(_tempStoragePath);
            }
        }
        catch { }
    }

    [Fact]
    public async Task GetTodayCurriculumAsync_GeneratesFiveCoreTasks()
    {
        // Arrange
        _mockReviewRepo.Setup(r => r.CountDueCardsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);
        _mockReviewRepo.Setup(r => r.GetDifficultCardsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReviewCard> { new() { Id = Guid.NewGuid(), Lapses = 3 } });
        _mockReviewRepo.Setup(r => r.GetCardCountsByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<LearningStatus, int>
            {
                [LearningStatus.New] = 40,
                [LearningStatus.Mastered] = 60
            });
        _mockGrammarRepo.Setup(g => g.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GrammarRule> { new() { Id = Guid.NewGuid(), Title = "Past Simple" } });
        _mockGrammarRepo.Setup(g => g.CountAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var curriculum = await _plannerService.GetTodayCurriculumAsync();

        // Assert
        curriculum.Should().NotBeNull();
        curriculum.Tasks.Should().HaveCount(5);
        curriculum.Tasks.Should().Contain(t => t.Type == CurriculumTaskType.ReviewDueCards);
        curriculum.Tasks.Should().Contain(t => t.Type == CurriculumTaskType.LearnNewWords);
        curriculum.Tasks.Should().Contain(t => t.Type == CurriculumTaskType.PracticeWeakWords);
        curriculum.Tasks.Should().Contain(t => t.Type == CurriculumTaskType.StudyGrammar);
        curriculum.Tasks.Should().Contain(t => t.Type == CurriculumTaskType.TakeAdaptiveQuiz);
    }

    [Fact]
    public async Task SaveAndGetUserGoalAsync_PersistsAndCalculatesRemainingDays()
    {
        // Arrange
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45));
        var goal = new StudyGoalDto(
            TargetExam: ExamType.Ielts,
            TargetScore: "7.0",
            TargetDate: targetDate,
            DailyMinutesGoal: 30,
            DailyNewWordsGoal: 15,
            DailyReviewTarget: 25,
            IsConfigured: true
        );

        // Act
        await _plannerService.SaveUserGoalAsync(goal);
        var retrieved = await _plannerService.GetUserGoalAsync();

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.TargetExam.Should().Be(ExamType.Ielts);
        retrieved.TargetScore.Should().Be("7.0");
        retrieved.DailyNewWordsGoal.Should().Be(15);
        retrieved.IsConfigured.Should().BeTrue();
    }
}
