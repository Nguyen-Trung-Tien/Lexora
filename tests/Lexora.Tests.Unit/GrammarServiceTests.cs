using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class GrammarServiceTests
{
    private readonly Mock<IGrammarRepository> _mockGrammarRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly GrammarService _grammarService;

    public GrammarServiceTests()
    {
        _mockGrammarRepo = new Mock<IGrammarRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _grammarService = new GrammarService(_mockGrammarRepo.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task CreateRuleAsync_ShouldCreateRuleWithExamplesAndPersist()
    {
        // Arrange
        var dto = new GrammarRuleCreateOrUpdateDto(
            Id: null,
            Title: "Past Perfect Tense",
            Structure: "S + had + V3/ed",
            ExplanationVi: "Diễn tả hành động xảy ra trước một hành động khác trong quá khứ.",
            ExplanationEn: "Describes an action completed before another past action.",
            CommonMistakes: "Confusing with Past Simple when time order is clear.",
            Level: CefrLevel.B1,
            ExamTarget: ExamType.Toeic,
            Topic: "Tenses",
            Examples:
            [
                new("She had left before I arrived.", "Cô ấy đã rời đi trước khi tôi đến.", "Past Perfect vs Past Simple")
            ]
        );

        _mockGrammarRepo.Setup(r => r.AddAsync(It.IsAny<GrammarRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GrammarRule r, CancellationToken _) => r);

        // Act
        var result = await _grammarService.CreateRuleAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Past Perfect Tense");
        result.Structure.Should().Be("S + had + V3/ed");
        result.Level.Should().Be(CefrLevel.B1);
        result.ExamTarget.Should().Be(ExamType.Toeic);
        result.Examples.Should().HaveCount(1);
        result.Examples[0].SentenceEn.Should().Be("She had left before I arrived.");

        _mockGrammarRepo.Verify(r => r.AddAsync(It.IsAny<GrammarRule>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRuleAsync_ShouldUpdateRuleAndSyncExamples()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var existingRule = new GrammarRule
        {
            Id = ruleId,
            Title = "Old Title",
            Structure = "Old Structure",
            ExplanationVi = "Old Explanation",
            Examples = new List<GrammarExample>
            {
                new() { Id = Guid.NewGuid(), SentenceEn = "Old example", SentenceVi = "Ví dụ cũ" }
            }
        };

        _mockGrammarRepo.Setup(r => r.GetWithExamplesAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRule);

        var updateDto = new GrammarRuleCreateOrUpdateDto(
            Id: ruleId,
            Title: "New Title",
            Structure: "New Structure",
            ExplanationVi: "New Explanation",
            ExplanationEn: null,
            CommonMistakes: null,
            Level: CefrLevel.B2,
            ExamTarget: ExamType.Ielts,
            Topic: "Advanced Tenses",
            Examples:
            [
                new("New example 1", "Ví dụ mới 1", null),
                new("New example 2", "Ví dụ mới 2", null)
            ]
        );

        // Act
        var result = await _grammarService.UpdateRuleAsync(updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Title");
        result.Structure.Should().Be("New Structure");
        result.Level.Should().Be(CefrLevel.B2);

        _mockGrammarRepo.Verify(r => r.RemoveExamples(It.IsAny<IEnumerable<GrammarExample>>()), Times.Once);
        _mockGrammarRepo.Verify(r => r.AddExamples(It.IsAny<IEnumerable<GrammarExample>>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRuleAsync_WhenRuleExists_ShouldDelete()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = new GrammarRule { Id = ruleId, Title = "To be deleted" };

        _mockGrammarRepo.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        await _grammarService.DeleteRuleAsync(ruleId);

        // Assert
        _mockGrammarRepo.Verify(r => r.DeleteAsync(rule, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateGrammarQuizAsync_ShouldProduceQuestionsWithFourChoices()
    {
        // Arrange
        var sampleRules = new List<GrammarRule>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Conditional Type 1",
                Structure = "If + S + V(s/es), S + will + V-inf",
                ExplanationVi = "Điều kiện có thật ở hiện tại hoặc tương lai",
                Level = CefrLevel.A2,
                Examples = [new() { SentenceEn = "If it rains, we will stay at home.", SentenceVi = "Nếu trời mưa, chúng tôi sẽ ở nhà." }]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Conditional Type 2",
                Structure = "If + S + V2/ed, S + would + V-inf",
                ExplanationVi = "Điều kiện không có thật ở hiện tại",
                Level = CefrLevel.B1,
                Examples = [new() { SentenceEn = "If I had money, I would buy a car.", SentenceVi = "Nếu tôi có tiền, tôi sẽ mua ô tô." }]
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Passive Voice (Present Simple)",
                Structure = "S + am/is/are + V3/ed",
                ExplanationVi = "Câu bị động ở thì hiện tại đơn",
                Level = CefrLevel.A2
            }
        };

        _mockGrammarRepo.Setup(r => r.GetFilteredAsync(
            It.IsAny<CefrLevel?>(),
            It.IsAny<ExamType?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleRules);

        // Act
        var questions = await _grammarService.GenerateGrammarQuizAsync(count: 3);

        // Assert
        questions.Should().HaveCount(3);
        foreach (var q in questions)
        {
            q.Options.Should().HaveCount(4);
            q.Options.Should().Contain(q.CorrectAnswer);
            q.Prompt.Should().NotBeNullOrWhiteSpace();
            q.Explanation.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void EvaluateQuiz_ShouldCalculateScoreAndTrackAnswerCorrectness()
    {
        // Arrange
        var q1 = new GrammarQuizQuestionDto(Guid.NewGuid(), 1, "Prompt 1", null, "Option A", ["Option A", "B", "C", "D"], "Exp 1");
        var q2 = new GrammarQuizQuestionDto(Guid.NewGuid(), 2, "Prompt 2", null, "Option B", ["Option A", "B", "C", "D"], "Exp 2");

        var questions = new List<GrammarQuizQuestionDto> { q1, q2 };
        var answers = new List<GrammarQuizSubmissionDto>
        {
            new(q1.RuleId, 1, "Option A"), // Correct
            new(q2.RuleId, 2, "Wrong Answer") // Incorrect
        };

        // Act
        var result = _grammarService.EvaluateQuiz(questions, answers);

        // Assert
        result.TotalQuestions.Should().Be(2);
        result.CorrectCount.Should().Be(1);
        result.IncorrectCount.Should().Be(1);
        result.ScorePercentage.Should().Be(50.0);
        result.Questions[0].IsCorrect.Should().BeTrue();
        result.Questions[1].IsCorrect.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRulesAsync_ShouldDeleteMultipleRulesAndCommitOnce()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var rule1 = new GrammarRule { Title = "Rule 1", Structure = "S + V", ExplanationVi = "Ex Vi", ExplanationEn = "Ex En", CommonMistakes = "Mistake", Level = CefrLevel.A1, ExamTarget = ExamType.Ielts, Topic = "Grammar" };
        var rule2 = new GrammarRule { Title = "Rule 2", Structure = "S + V2", ExplanationVi = "Ex Vi", ExplanationEn = "Ex En", CommonMistakes = "Mistake", Level = CefrLevel.A2, ExamTarget = ExamType.Ielts, Topic = "Grammar" };

        _mockGrammarRepo.Setup(r => r.GetByIdAsync(id1, It.IsAny<CancellationToken>())).ReturnsAsync(rule1);
        _mockGrammarRepo.Setup(r => r.GetByIdAsync(id2, It.IsAny<CancellationToken>())).ReturnsAsync(rule2);

        // Act
        var deletedCount = await _grammarService.DeleteRulesAsync([id1, id2]);

        // Assert
        deletedCount.Should().Be(2);
        _mockGrammarRepo.Verify(r => r.DeleteAsync(rule1, It.IsAny<CancellationToken>()), Times.Once);
        _mockGrammarRepo.Verify(r => r.DeleteAsync(rule2, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
