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

public class VocabularyCrudIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LexoraDbContext _context;
    private readonly VocabularyRepository _vocabRepo;
    private readonly ReviewRepository _reviewRepo;
    private readonly UnitOfWork _unitOfWork;
    private readonly VocabularyService _vocabService;

    public VocabularyCrudIntegrationTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(_connection)
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
            .Options;

        _context = new LexoraDbContext(options);
        _context.Database.EnsureCreated();

        _vocabRepo = new VocabularyRepository(_context);
        _reviewRepo = new ReviewRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
        _vocabService = new VocabularyService(_vocabRepo, _reviewRepo, _unitOfWork);
    }

    [Fact]
    public async Task FullCrudCycle_WithMultipleMeaningsAndCascades_ShouldSucceed()
    {
        // 1. CREATE
        var createDto = new WordCreateOrUpdateDto(
            Id: null,
            Term: "pragmatic",
            PhoneticUk: "/præɡˈmæt.ɪk/",
            PhoneticUs: "/præɡˈmæt̬.ɪk/",
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.C1,
            PrimaryPartOfSpeech: PartOfSpeech.Adjective,
            Notes: "Practical over theoretical",
            Meanings: new List<WordMeaningCreateDto>
            {
                new(
                    PartOfSpeech: PartOfSpeech.Adjective,
                    DefinitionVi: "thực tế, thực dụng",
                    DefinitionEn: "solving problems in a sensible way that suits the conditions",
                    Explanation: "Used in business and decision making",
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("We need a pragmatic solution.", "Chúng ta cần một giải pháp thực tế.", null),
                        new("A pragmatic approach to politics.", "Một cách tiếp cận thực dụng đối với chính trị.", null)
                    }
                ),
                new(
                    PartOfSpeech: PartOfSpeech.Noun,
                    DefinitionVi: "người thực tế (hiếm gặp)",
                    DefinitionEn: "a pragmatic person",
                    Explanation: "Archaic / rare noun usage",
                    Examples: new List<ExampleSentenceCreateDto>()
                )
            },
            TopicIds: new List<Guid>(),
            ExamTagIds: new List<Guid>()
        );

        var created = await _vocabService.CreateWordAsync(createDto);

        created.Should().NotBeNull();
        created.Term.Should().Be("pragmatic");
        created.Meanings.Should().HaveCount(2);
        created.Meanings[0].Examples.Should().HaveCount(2);

        // Verify ReviewCard created automatically
        var cardInDb = await _context.ReviewCards.FirstOrDefaultAsync(rc => rc.WordId == created.Id);
        cardInDb.Should().NotBeNull();
        cardInDb!.Status.Should().Be(LearningStatus.New);

        // 2. READ & SEARCH
        var fetched = await _vocabService.GetWordByIdAsync(created.Id);
        fetched.Should().NotBeNull();
        fetched!.Term.Should().Be("pragmatic");

        var searchResults = await _vocabService.SearchWordsAsync(search: "thực dụng");
        searchResults.Should().ContainSingle(w => w.Term == "pragmatic");

        // 3. UPDATE
        var updateDto = new WordCreateOrUpdateDto(
            Id: created.Id,
            Term: "pragmatic",
            PhoneticUk: "/præɡˈmæt.ɪk/",
            PhoneticUs: "/præɡˈmæt̬.ɪk/",
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.C1,
            PrimaryPartOfSpeech: PartOfSpeech.Adjective,
            Notes: "Updated note: Highly recommended for IELTS C1",
            Meanings: new List<WordMeaningCreateDto>
            {
                new(
                    PartOfSpeech: PartOfSpeech.Adjective,
                    DefinitionVi: "thực tế, thực tiễn",
                    DefinitionEn: "dealing with things sensibly and realistically",
                    Explanation: "Updated explanation",
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("His pragmatic attitude helped resolve the crisis.", "Thái độ thực tế của anh ấy đã giúp giải quyết cuộc khủng hoảng.", null)
                    }
                )
            },
            TopicIds: new List<Guid>(),
            ExamTagIds: new List<Guid>()
        );

        var updated = await _vocabService.UpdateWordAsync(updateDto);
        updated.Notes.Should().Be("Updated note: Highly recommended for IELTS C1");
        updated.Meanings.Should().HaveCount(1);
        updated.Meanings[0].DefinitionVi.Should().Be("thực tế, thực tiễn");

        // 4. DELETE & CASCADE
        await _vocabService.DeleteWordAsync(created.Id);

        var wordAfterDelete = await _context.Words.FindAsync(created.Id);
        wordAfterDelete.Should().BeNull();

        var meaningsAfterDelete = await _context.WordMeanings.Where(m => m.WordId == created.Id).ToListAsync();
        meaningsAfterDelete.Should().BeEmpty();

        var cardAfterDelete = await _context.ReviewCards.FirstOrDefaultAsync(rc => rc.WordId == created.Id);
        cardAfterDelete.Should().BeNull();
    }

    [Fact]
    public async Task ReviewSession_Integration_FullLifecycle_ShouldPersistLogsAndStreaks()
    {
        // 1. Create a word with review card
        var createDto = new WordCreateOrUpdateDto(
            Id: null,
            Term: "serendipity",
            PhoneticUk: "/ˌser.ənˈdɪp.ə.ti/",
            PhoneticUs: "/ˌser.ənˈdɪp.ə.t̬i/",
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.C2,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: "Finding interesting things by chance",
            Meanings: new List<WordMeaningCreateDto>
            {
                new(
                    PartOfSpeech: PartOfSpeech.Noun,
                    DefinitionVi: "sự tình cờ may mắn",
                    DefinitionEn: "good luck in making unexpected discoveries",
                    Explanation: null,
                    Examples: new List<ExampleSentenceCreateDto>()
                )
            },
            TopicIds: new List<Guid>(),
            ExamTagIds: new List<Guid>()
        );

        var wordDto = await _vocabService.CreateWordAsync(createDto);
        var reviewService = new ReviewService(_reviewRepo, new Lexora.Application.SpacedRepetition.Sm2SpacedRepetitionEngine(), _unitOfWork);

        // 2. Start session
        var sessionDto = await reviewService.StartSessionAsync(ReviewSessionMode.TodayDue, 10);
        sessionDto.Should().NotBeNull();

        // 3. Find card
        var dueCards = await reviewService.GetDueCardsAsync();
        dueCards.Should().Contain(c => c.WordId == wordDto.Id);
        var card = dueCards.First(c => c.WordId == wordDto.Id);

        // 4. Submit review rating 'Good'
        var reviewResult = await reviewService.SubmitReviewAsync(card.Id, ReviewRating.Good, responseTimeMs: 1800, sessionId: sessionDto.Id);
        reviewResult.NewStatus.Should().Be(LearningStatus.Remembered);

        // 5. Complete session
        var completedSession = await reviewService.CompleteSessionAsync(sessionDto.Id, totalCards: 1, correctCount: 1, incorrectCount: 0);
        completedSession.AccuracyRate.Should().Be(100.0);

        // 6. Verify SQLite persistence of StudyLog
        var logInDb = await _context.StudyLogs.FirstOrDefaultAsync(l => l.StudySessionId == sessionDto.Id);
        logInDb.Should().NotBeNull();
        logInDb!.Rating.Should().Be(ReviewRating.Good);
        logInDb.ResponseTimeMs.Should().Be(1800);

        // 7. Verify Streak was updated
        var streakInDb = await _context.UserStreaks.FirstOrDefaultAsync();
        streakInDb.Should().NotBeNull();
        streakInDb!.CurrentStreak.Should().Be(1);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
