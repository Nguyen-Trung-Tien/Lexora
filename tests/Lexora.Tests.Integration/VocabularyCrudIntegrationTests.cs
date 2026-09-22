using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Application.SpacedRepetition;
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

    [Fact]
    public async Task MarkStatusQuickAsync_ShouldUpdateStatusAndLogCorrectly()
    {
        // 1. Arrange word
        var createDto = new WordCreateOrUpdateDto(
            Id: null,
            Term: "quicktest",
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B1,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: null,
            Meanings: new List<WordMeaningCreateDto>
            {
                new(PartOfSpeech.Noun, "bài kiểm tra nhanh", null, null, new List<ExampleSentenceCreateDto>())
            },
            TopicIds: new List<Guid>(),
            ExamTagIds: new List<Guid>()
        );

        var word = await _vocabService.CreateWordAsync(createDto);
        var reviewService = new ReviewService(_reviewRepo, new Sm2SpacedRepetitionEngine(), _unitOfWork);

        // 2. Act: Mark remembered = true
        await reviewService.MarkStatusQuickAsync(word.Id, remembered: true);

        // 3. Assert
        var card = await _reviewRepo.GetByWordIdAsync(word.Id);
        card.Should().NotBeNull();
        card!.Status.Should().Be(LearningStatus.Remembered);

        // 4. Act: Mark remembered = false (Chưa nhớ)
        await reviewService.MarkStatusQuickAsync(word.Id, remembered: false);

        var cardUpdated = await _reviewRepo.GetByWordIdAsync(word.Id);
        cardUpdated!.Status.Should().Be(LearningStatus.NotRemembered);
    }

    [Fact]
    public async Task GetContextUnitsAsync_ShouldReturnGroupedUnitsWithMasteryMetrics()
    {
        // 1. Create a Topic
        var topic = new Lexora.Domain.Entities.Topic
        {
            Id = Guid.NewGuid(),
            Name = "Work & Career",
            Description = "Vocabulary for professional workplace contexts",
            IconGlyph = "\uE8F1",
            ColorHex = "#4A90E2"
        };
        _context.Topics.Add(topic);
        await _unitOfWork.SaveChangesAsync();

        // 2. Create Word 1 with topic
        var word1Dto = new WordCreateOrUpdateDto(
            Id: null,
            Term: "negotiation",
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B2,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: null,
            Meanings: new List<WordMeaningCreateDto> { new(PartOfSpeech.Noun, "đàm phán", null, null, new List<ExampleSentenceCreateDto>()) },
            TopicIds: new List<Guid> { topic.Id },
            ExamTagIds: new List<Guid>()
        );
        var word1 = await _vocabService.CreateWordAsync(word1Dto);

        // 3. Create Word 2 with topic
        var word2Dto = new WordCreateOrUpdateDto(
            Id: null,
            Term: "contract",
            PhoneticUk: null,
            PhoneticUs: null,
            Level: CefrLevel.B1,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Meanings: new List<WordMeaningCreateDto> { new(PartOfSpeech.Noun, "hợp đồng", null, null, new List<ExampleSentenceCreateDto>()) },
            TopicIds: new List<Guid> { topic.Id },
            ExamTagIds: new List<Guid>()
        );
        var word2 = await _vocabService.CreateWordAsync(word2Dto);

        // 4. Create Word 3 without topic (untagged)
        var word3Dto = new WordCreateOrUpdateDto(
            Id: null,
            Term: "serendipity",
            PhoneticUk: null,
            PhoneticUs: null,
            Level: CefrLevel.C2,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Meanings: new List<WordMeaningCreateDto> { new(PartOfSpeech.Noun, "sự tình cờ may mắn", null, null, new List<ExampleSentenceCreateDto>()) },
            TopicIds: new List<Guid>(),
            ExamTagIds: new List<Guid>()
        );
        await _vocabService.CreateWordAsync(word3Dto);

        // 5. Mark word1 as Remembered
        var reviewService = new ReviewService(_reviewRepo, new Sm2SpacedRepetitionEngine(), _unitOfWork);
        await reviewService.MarkStatusQuickAsync(word1.Id, remembered: true);

        // 6. Act: Retrieve context units
        var units = await _vocabService.GetContextUnitsAsync();

        // 7. Assert
        units.Should().NotBeNull();
        var careerUnit = units.FirstOrDefault(u => u.Id == topic.Id);
        careerUnit.Should().NotBeNull();
        careerUnit!.Name.Should().Be("Work & Career");
        careerUnit.TotalWords.Should().Be(2);
        careerUnit.RememberedWords.Should().Be(1);
        careerUnit.NotRememberedWords.Should().Be(0);
        careerUnit.MasteryPercentage.Should().Be(50.0);
        careerUnit.Words.Should().HaveCount(2);

        var generalUnit = units.FirstOrDefault(u => u.Id == Guid.Empty);
        generalUnit.Should().NotBeNull();
        generalUnit!.TotalWords.Should().Be(1);
        generalUnit.Words.Any(w => w.Term == "serendipity").Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdsWithDetailsAsync_ShouldReturnAllRequestedWordsInSingleBatch()
    {
        // Arrange
        var w1 = await _vocabService.CreateWordAsync(new WordCreateOrUpdateDto(
            null, "tenacious", "/təˈneɪ.ʃəs/", null, null, null, CefrLevel.C1, PartOfSpeech.Adjective, null,
            new List<WordMeaningCreateDto> { new(PartOfSpeech.Adjective, "kiên trì, bền bỉ", null, null, new List<ExampleSentenceCreateDto>()) },
            new List<Guid>(), new List<Guid>()));

        var w2 = await _vocabService.CreateWordAsync(new WordCreateOrUpdateDto(
            null, "meticulous", "/məˈtɪk.jə.ləs/", null, null, null, CefrLevel.C1, PartOfSpeech.Adjective, null,
            new List<WordMeaningCreateDto> { new(PartOfSpeech.Adjective, "tỉ mỉ, kỹ lưỡng", null, null, new List<ExampleSentenceCreateDto>()) },
            new List<Guid>(), new List<Guid>()));

        // Act
        var batchWords = await _vocabRepo.GetByIdsWithDetailsAsync(new[] { w1.Id, w2.Id });

        // Assert
        batchWords.Should().HaveCount(2);
        batchWords.Select(w => w.Term).Should().Contain(new[] { "tenacious", "meticulous" });
        batchWords.All(w => w.Meanings.Count > 0).Should().BeTrue();
        batchWords.All(w => w.ReviewCard != null).Should().BeTrue();
    }

    [Fact]
    public async Task VocabularyUpdate_ConsecutiveUpdates_ShouldRetainAudioAndSyncChildren()
    {
        // 1. Arrange topics and exam tags
        var topic1 = await _vocabService.CreateTopicAsync(new TopicCreateOrUpdateDto("Business", "Corporate vocabulary", "\uE8EC", "#0078D4"));
        var topic2 = await _vocabService.CreateTopicAsync(new TopicCreateOrUpdateDto("Tech", "IT vocabulary", "\uE8EC", "#107C41"));

        // 2. Create word with audio and initial meaning
        var created = await _vocabService.CreateWordAsync(new WordCreateOrUpdateDto(
            Id: null,
            Term: "lucrative",
            PhoneticUk: "/ˈluː.krə.tɪv/",
            PhoneticUs: "/ˈluː.krə.t̬ɪv/",
            AudioUkPath: "audio/cache/uk_lucrative.mp3",
            AudioUsPath: "audio/cache/us_lucrative.mp3",
            Level: CefrLevel.C1,
            PrimaryPartOfSpeech: PartOfSpeech.Adjective,
            Notes: "Initial note",
            Meanings: new List<WordMeaningCreateDto>
            {
                new(
                    PartOfSpeech: PartOfSpeech.Adjective,
                    DefinitionVi: "sinh lợi, có nhiều lợi nhuận",
                    DefinitionEn: "producing a great deal of profit",
                    Explanation: null,
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("A lucrative contract.", "Một hợp đồng béo bở.", null)
                    }
                )
            },
            TopicIds: new List<Guid> { topic1.Id },
            ExamTagIds: new List<Guid>()
        ));

        created.AudioUkPath.Should().Be("audio/cache/uk_lucrative.mp3");
        created.AudioUsPath.Should().Be("audio/cache/us_lucrative.mp3");
        created.Meanings.Should().HaveCount(1);
        created.Topics.Should().Contain("Business");

        // 3. First update: preserve audio, update topic to Tech, add second meaning
        var update1 = await _vocabService.UpdateWordAsync(new WordCreateOrUpdateDto(
            Id: created.Id,
            Term: "lucrative",
            PhoneticUk: created.PhoneticUk,
            PhoneticUs: created.PhoneticUs,
            AudioUkPath: created.AudioUkPath,
            AudioUsPath: created.AudioUsPath,
            Level: CefrLevel.C1,
            PrimaryPartOfSpeech: PartOfSpeech.Adjective,
            Notes: "Updated note 1",
            Meanings: new List<WordMeaningCreateDto>
            {
                new(
                    PartOfSpeech: PartOfSpeech.Adjective,
                    DefinitionVi: "sinh lời, béo bở",
                    DefinitionEn: "profitable",
                    Explanation: "Used for business ventures",
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("Software development is a lucrative career.", "Phát triển phần mềm là một nghề sinh lời.", null)
                    }
                ),
                new(
                    PartOfSpeech: PartOfSpeech.Noun,
                    DefinitionVi: "sự sinh lợi (nghĩa hiếm)",
                    DefinitionEn: null,
                    Explanation: null,
                    Examples: new List<ExampleSentenceCreateDto>()
                )
            },
            TopicIds: new List<Guid> { topic2.Id },
            ExamTagIds: new List<Guid>()
        ));

        update1.AudioUkPath.Should().Be("audio/cache/uk_lucrative.mp3");
        update1.AudioUsPath.Should().Be("audio/cache/us_lucrative.mp3");
        update1.Meanings.Should().HaveCount(2);
        update1.Topics.Should().Contain("Tech");

        // Verify direct from DB query
        var dbFetch1 = await _vocabService.GetWordByIdAsync(created.Id);
        dbFetch1.Should().NotBeNull();
        dbFetch1!.Meanings.Should().HaveCount(2);
        dbFetch1.Topics.Should().Contain("Tech");
        dbFetch1.AudioUkPath.Should().Be("audio/cache/uk_lucrative.mp3");

        // 4. Second update: remove second meaning, switch back to topic1
        var update2 = await _vocabService.UpdateWordAsync(new WordCreateOrUpdateDto(
            Id: created.Id,
            Term: "lucrative",
            PhoneticUk: created.PhoneticUk,
            PhoneticUs: created.PhoneticUs,
            AudioUkPath: created.AudioUkPath,
            AudioUsPath: created.AudioUsPath,
            Level: CefrLevel.C2,
            PrimaryPartOfSpeech: PartOfSpeech.Adjective,
            Notes: "Final note",
            Meanings: new List<WordMeaningCreateDto>
            {
                new(
                    PartOfSpeech: PartOfSpeech.Adjective,
                    DefinitionVi: "sinh lời cao",
                    DefinitionEn: "highly profitable",
                    Explanation: null,
                    Examples: new List<ExampleSentenceCreateDto>()
                )
            },
            TopicIds: new List<Guid> { topic1.Id },
            ExamTagIds: new List<Guid>()
        ));

        update2.Level.Should().Be(CefrLevel.C2);
        update2.Meanings.Should().HaveCount(1);
        update2.Meanings[0].DefinitionVi.Should().Be("sinh lời cao");
        update2.Topics.Should().Contain("Business");
        update2.AudioUkPath.Should().Be("audio/cache/uk_lucrative.mp3");

        var dbFetch2 = await _vocabService.GetWordByIdAsync(created.Id);
        dbFetch2.Should().NotBeNull();
        dbFetch2!.Meanings.Should().HaveCount(1);
        dbFetch2.Topics.Should().Contain("Business");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
