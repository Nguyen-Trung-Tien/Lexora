using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Enums;
using Lexora.Infrastructure.Data;
using Lexora.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexora.Tests.Integration;

public class TestPracticeIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LexoraDbContext _context;
    private readonly VocabularyRepository _vocabRepo;
    private readonly ReviewRepository _reviewRepo;
    private readonly UnitOfWork _unitOfWork;
    private readonly TestService _testService;

    public TestPracticeIntegrationTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LexoraDbContext(options);

        var initializer = new SqliteDatabaseInitializer(_context, NullLogger<SqliteDatabaseInitializer>.Instance);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _vocabRepo = new VocabularyRepository(_context);
        _reviewRepo = new ReviewRepository(_context);
        _unitOfWork = new UnitOfWork(_context);

        _testService = new TestService(_vocabRepo, _reviewRepo, _unitOfWork);
    }

    [Fact]
    public async Task GenerateAndSubmitTest_Integration_ShouldPersistStudySessionAndLogs()
    {
        // 1. Generate Test from real seeded database
        var config = new TestConfigurationDto(
            Type: TestType.Mixed,
            QuestionCount: 5
        );

        var session = await _testService.GenerateTestAsync(config);
        session.Questions.Should().HaveCount(5);

        // Verify session was persisted in DB
        var savedSession = await _context.StudySessions.FindAsync(session.SessionId);
        savedSession.Should().NotBeNull();
        savedSession!.SessionType.Should().Be("Test_Mixed");
        savedSession.TotalCards.Should().Be(5);
        savedSession.CompletedAt.Should().BeNull();

        // 2. Submit Answers: 3 correct, 2 incorrect
        var answers = new List<TestAnswerSubmissionDto>
        {
            new(session.Questions[0].WordId, session.Questions[0].QuestionNumber, session.Questions[0].CorrectAnswer),
            new(session.Questions[1].WordId, session.Questions[1].QuestionNumber, session.Questions[1].CorrectAnswer),
            new(session.Questions[2].WordId, session.Questions[2].QuestionNumber, session.Questions[2].CorrectAnswer),
            new(session.Questions[3].WordId, session.Questions[3].QuestionNumber, "DeliberateWrongAnswer"),
            new(session.Questions[4].WordId, session.Questions[4].QuestionNumber, "DeliberateWrongAnswer")
        };

        var result = await _testService.SubmitTestAsync(session.SessionId, answers);

        // 3. Verify Result Calculation
        result.TotalQuestions.Should().Be(5);
        result.CorrectCount.Should().Be(3);
        result.IncorrectCount.Should().Be(2);
        result.ScorePercentage.Should().Be(60.0);
        result.WeakWords.Should().HaveCount(2);

        // 4. Verify Database Persistence of Results
        var completedDbSession = await _context.StudySessions.Include(s => s.StudyLogs).FirstOrDefaultAsync(s => s.Id == session.SessionId);
        completedDbSession.Should().NotBeNull();
        completedDbSession!.CompletedAt.Should().NotBeNull();
        completedDbSession.CorrectCards.Should().Be(3);
        completedDbSession.IncorrectCards.Should().Be(2);
        completedDbSession.StudyLogs.Should().HaveCount(5);

        // Verify streak was updated
        var streak = await _context.UserStreaks.FirstOrDefaultAsync();
        streak.Should().NotBeNull();
        streak!.CurrentStreak.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task WeakWords_ShouldBeQueryableAfterFailingTests()
    {
        // 1. Generate test
        var config = new TestConfigurationDto(Type: TestType.WordToMeaning, QuestionCount: 3);
        var session = await _testService.GenerateTestAsync(config);

        // 2. Fail all questions
        var answers = session.Questions.Select(q => new TestAnswerSubmissionDto(
            q.WordId,
            q.QuestionNumber,
            "WrongAnswer"
        )).ToList();

        var result = await _testService.SubmitTestAsync(session.SessionId, answers);
        result.WeakWords.Should().HaveCount(3);

        // 3. Query weak words from service
        var weakWords = await _testService.GetWeakWordsAsync(10);
        weakWords.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CustomWordList_SingleWordBoundary_ShouldGenerateSingleQuestionWithDistractors()
    {
        // 1. Arrange: Pick a single word from DB
        var singleWord = await _context.Words.Include(w => w.Meanings).FirstAsync();
        var config = new TestConfigurationDto(
            Type: TestType.WordToMeaning,
            QuestionCount: 20, // Intentionally requested more questions than available custom words
            CustomWordIds: new List<Guid> { singleWord.Id }
        );

        // 2. Act: Generate test
        var session = await _testService.GenerateTestAsync(config);

        // 3. Assert: Exactly 1 question is generated without boundary exception
        session.Questions.Should().HaveCount(1);
        var q = session.Questions[0];
        q.WordId.Should().Be(singleWord.Id);
        q.Options.Should().HaveCount(4);
        q.Options.Should().Contain(q.CorrectAnswer);

        // 4. Submit correct answer
        var answers = new List<TestAnswerSubmissionDto>
        {
            new(q.WordId, q.QuestionNumber, q.CorrectAnswer)
        };
        var result = await _testService.SubmitTestAsync(session.SessionId, answers);

        result.TotalQuestions.Should().Be(1);
        result.CorrectCount.Should().Be(1);
        result.ScorePercentage.Should().Be(100.0);
        result.WeakWords.Should().BeEmpty();
    }

    [Fact]
    public async Task ContextUnit_DirectTestFlow_ShouldSeamlesslyIntegrateWithCustomTest()
    {
        // 1. Arrange: Use VocabularyService to get real Context Units
        var vocabService = new VocabularyService(_vocabRepo, _reviewRepo, _unitOfWork);
        var units = await vocabService.GetContextUnitsAsync();
        units.Should().NotBeEmpty();

        var testUnit = units.First(u => u.Words.Count > 0);

        // 2. Act: Generate test directly from Context Unit words
        var config = new TestConfigurationDto(
            Type: TestType.Mixed,
            QuestionCount: testUnit.Words.Count,
            CustomWordIds: testUnit.Words.Select(w => w.Id).ToList()
        );

        var session = await _testService.GenerateTestAsync(config);

        // 3. Assert: Questions match unit words
        session.Questions.Should().HaveCount(testUnit.Words.Count);
        var testWordIds = testUnit.Words.Select(w => w.Id).ToHashSet();
        foreach (var q in session.Questions)
        {
            testWordIds.Should().Contain(q.WordId);
            q.Options.Should().HaveCount(4);
        }
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
