using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class TestServiceTests
{
    private readonly Mock<IVocabularyRepository> _mockVocabRepo;
    private readonly Mock<IReviewRepository> _mockReviewRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly TestService _testService;
    private readonly List<Word> _sampleWords;

    public TestServiceTests()
    {
        _mockVocabRepo = new Mock<IVocabularyRepository>();
        _mockReviewRepo = new Mock<IReviewRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _sampleWords = CreateSampleWords();

        _mockVocabRepo.Setup(r => r.SearchAsync(
            It.IsAny<string?>(),
            It.IsAny<CefrLevel?>(),
            It.IsAny<PartOfSpeech?>(),
            It.IsAny<Guid?>(),
            It.IsAny<ExamType?>(),
            It.IsAny<LearningStatus?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(_sampleWords);

        _mockReviewRepo.Setup(r => r.CreateStudySessionAsync(It.IsAny<StudySession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudySession s, CancellationToken _) => s);

        _mockReviewRepo.Setup(r => r.GetStudySessionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new StudySession { Id = id, StartedAt = DateTime.UtcNow, SessionType = "Test_Mixed" });

        _testService = new TestService(_mockVocabRepo.Object, _mockReviewRepo.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GenerateTestAsync_WordToMeaning_ShouldGenerateFourDistinctOptionsIncludingCorrectAnswer()
    {
        // Arrange
        var config = new TestConfigurationDto(
            Type: TestType.WordToMeaning,
            QuestionCount: 5
        );

        // Act
        var session = await _testService.GenerateTestAsync(config);

        // Assert
        session.Questions.Should().HaveCount(5);
        foreach (var q in session.Questions)
        {
            q.QuestionType.Should().Be(TestType.WordToMeaning);
            q.Options.Should().HaveCount(4);
            q.Options.Distinct().Should().HaveCount(4, "options should not contain duplicates");
            q.Options.Should().Contain(q.CorrectAnswer);
            q.Prompt.Should().Contain(q.TargetWord.Term);
        }
    }

    [Fact]
    public async Task GenerateTestAsync_MeaningToWord_ShouldProvideEnglishTermsAsChoices()
    {
        // Arrange
        var config = new TestConfigurationDto(
            Type: TestType.MeaningToWord,
            QuestionCount: 3
        );

        // Act
        var session = await _testService.GenerateTestAsync(config);

        // Assert
        session.Questions.Should().HaveCount(3);
        foreach (var q in session.Questions)
        {
            q.QuestionType.Should().Be(TestType.MeaningToWord);
            q.CorrectAnswer.Should().Be(q.TargetWord.Term);
            q.Options.Should().Contain(q.TargetWord.Term);
            q.Prompt.Should().Contain(q.TargetWord.Meanings[0].DefinitionVi);
        }
    }

    [Fact]
    public async Task GenerateTestAsync_FillInTheBlank_ShouldBlankOutTargetWordInSentence()
    {
        // Arrange
        var config = new TestConfigurationDto(
            Type: TestType.FillInTheBlank,
            QuestionCount: 2
        );

        // Act
        var session = await _testService.GenerateTestAsync(config);

        // Assert
        session.Questions.Should().HaveCount(2);
        var q = session.Questions[0];
        q.QuestionType.Should().Be(TestType.FillInTheBlank);
        q.Prompt.Should().Contain("_______");
        q.CorrectAnswer.Should().Be(q.TargetWord.Term);
        q.Options.Should().Contain(q.CorrectAnswer);
    }

    [Fact]
    public async Task GenerateTestAsync_PartOfSpeech_ShouldHavePartOfSpeechChoices()
    {
        // Arrange
        var config = new TestConfigurationDto(
            Type: TestType.PartOfSpeech,
            QuestionCount: 3
        );

        // Act
        var session = await _testService.GenerateTestAsync(config);

        // Assert
        session.Questions.Should().HaveCount(3);
        foreach (var q in session.Questions)
        {
            q.QuestionType.Should().Be(TestType.PartOfSpeech);
            q.CorrectAnswer.Should().Be(q.TargetWord.PrimaryPartOfSpeech.ToString());
            q.Options.Should().Contain(q.CorrectAnswer);
        }
    }

    [Fact]
    public async Task SubmitTestAsync_WithAllCorrectAnswers_ShouldScoreHundredPercent()
    {
        // Arrange
        var config = new TestConfigurationDto(Type: TestType.WordToMeaning, QuestionCount: 3);
        var session = await _testService.GenerateTestAsync(config);

        var answers = session.Questions.Select(q => new TestAnswerSubmissionDto(
            q.WordId,
            q.QuestionNumber,
            q.CorrectAnswer,
            ResponseTimeMs: 1500
        )).ToList();

        // Act
        var result = await _testService.SubmitTestAsync(session.SessionId, answers);

        // Assert
        result.TotalQuestions.Should().Be(3);
        result.CorrectCount.Should().Be(3);
        result.IncorrectCount.Should().Be(0);
        result.ScorePercentage.Should().Be(100.0);
        result.WeakWords.Should().BeEmpty();
        _mockReviewRepo.Verify(r => r.UpdateStreakAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitTestAsync_WithIncorrectAnswers_ShouldIdentifyWeakWords()
    {
        // Arrange
        var config = new TestConfigurationDto(Type: TestType.WordToMeaning, QuestionCount: 3);
        var session = await _testService.GenerateTestAsync(config);

        // Answer only first question correctly, rest wrong
        var answers = new List<TestAnswerSubmissionDto>
        {
            new(session.Questions[0].WordId, session.Questions[0].QuestionNumber, session.Questions[0].CorrectAnswer),
            new(session.Questions[1].WordId, session.Questions[1].QuestionNumber, "Wrong answer"),
            new(session.Questions[2].WordId, session.Questions[2].QuestionNumber, "Wrong answer")
        };

        // Act
        var result = await _testService.SubmitTestAsync(session.SessionId, answers);

        // Assert
        result.TotalQuestions.Should().Be(3);
        result.CorrectCount.Should().Be(1);
        result.IncorrectCount.Should().Be(2);
        result.ScorePercentage.Should().BeApproximately(33.3, 0.5);
        result.WeakWords.Should().HaveCount(2);
        result.WeakWords.Select(w => w.Id).Should().Contain(session.Questions[1].WordId);
        result.WeakWords.Select(w => w.Id).Should().Contain(session.Questions[2].WordId);
    }

    private static List<Word> CreateSampleWords()
    {
        var list = new List<Word>();

        string[] terms = ["abandon", "resilient", "collaborate", "innovative", "meticulous", "pragmatic"];
        string[] meanings = ["từ bỏ, bỏ rơi", "kiên cường, phục hồi nhanh", "hợp tác", "mang tính sáng tạo, đổi mới", "tỉ mỉ, cẩn thận", "thực dụng, thực tế"];
        PartOfSpeech[] poses = [PartOfSpeech.Verb, PartOfSpeech.Adjective, PartOfSpeech.Verb, PartOfSpeech.Adjective, PartOfSpeech.Adjective, PartOfSpeech.Adjective];

        for (int i = 0; i < terms.Length; i++)
        {
            var wordId = Guid.NewGuid();
            var meaningId = Guid.NewGuid();

            var meaning = new WordMeaning
            {
                Id = meaningId,
                WordId = wordId,
                PartOfSpeech = poses[i],
                DefinitionVi = meanings[i],
                OrderIndex = 0,
                Examples = new List<ExampleSentence>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        WordMeaningId = meaningId,
                        SentenceEn = $"They decided to {terms[i]} the old plan.",
                        SentenceVi = $"Họ quyết định {meanings[i]} kế hoạch cũ."
                    }
                }
            };

            var word = new Word
            {
                Id = wordId,
                Term = terms[i],
                NormalizedTerm = terms[i].ToLowerInvariant(),
                Level = CefrLevel.B2,
                PrimaryPartOfSpeech = poses[i],
                PhoneticUs = $"/sample_{terms[i]}/",
                Meanings = [meaning]
            };

            list.Add(word);
        }

        return list;
    }
}
