using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities;
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

    [Fact]
    public async Task DeleteWordsAsync_BulkDeleteWithReviewCardsAndStudyLogs_ShouldDeleteAllAndCascadeCleanly()
    {
        // 1. Create two words
        var word1 = await _vocabService.CreateWordAsync(new WordCreateOrUpdateDto(
            Id: null,
            Term: "bulkdeleteone",
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B1,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: null,
            Meanings: new List<WordMeaningCreateDto>
            {
                new(PartOfSpeech.Noun, "nghĩa 1", "def 1", null, new List<ExampleSentenceCreateDto>())
            },
            TopicIds: new List<Guid>(),
            ExamTagIds: new List<Guid>()
        ));

        var word2 = await _vocabService.CreateWordAsync(new WordCreateOrUpdateDto(
            Id: null,
            Term: "bulkdeletetwo",
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B2,
            PrimaryPartOfSpeech: PartOfSpeech.Verb,
            Notes: null,
            Meanings: new List<WordMeaningCreateDto>
            {
                new(PartOfSpeech.Verb, "nghĩa 2", "def 2", null, new List<ExampleSentenceCreateDto>())
            },
            TopicIds: new List<Guid>(),
            ExamTagIds: new List<Guid>()
        ));

        // 2. Perform a review on word1 so it has StudyLogs
        var card1 = await _context.ReviewCards.FirstAsync(rc => rc.WordId == word1.Id);
        var reviewService = new ReviewService(_reviewRepo, new Lexora.Application.SpacedRepetition.Sm2SpacedRepetitionEngine(), _unitOfWork);
        await reviewService.SubmitReviewAsync(card1.Id, ReviewRating.Good, 1200);

        var logsBefore = await _context.StudyLogs.Where(sl => sl.ReviewCardId == card1.Id).ToListAsync();
        logsBefore.Should().NotBeEmpty();

        // 3. Bulk delete both words
        var deletedCount = await _vocabService.DeleteWordsAsync(new[] { word1.Id, word2.Id });
        deletedCount.Should().Be(2);

        // 4. Verify entities and related cascaded rows are gone
        var w1 = await _context.Words.FindAsync(word1.Id);
        var w2 = await _context.Words.FindAsync(word2.Id);
        w1.Should().BeNull();
        w2.Should().BeNull();

        var cards = await _context.ReviewCards.Where(rc => rc.WordId == word1.Id || rc.WordId == word2.Id).ToListAsync();
        cards.Should().BeEmpty();

        var logsAfter = await _context.StudyLogs.Where(sl => sl.ReviewCardId == card1.Id).ToListAsync();
        logsAfter.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAndEditWord_WithMultiPosHierarchy_ShouldPersistAtomicAndMaintainConsistency()
    {
        // 1. CREATE WORD WITH VERB & NOUN
        var createDto = new WordCreateOrUpdateDto(
            Id: null,
            Term: "run",
            PhoneticUk: "/rʌn/",
            PhoneticUs: "/rʌn/",
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.A1,
            PrimaryPartOfSpeech: PartOfSpeech.Verb,
            Notes: "Irregular verb: run - ran - run",
            Meanings: new List<WordMeaningCreateDto>
            {
                new(
                    PartOfSpeech: PartOfSpeech.Verb,
                    DefinitionVi: "chạy bằng chân với tốc độ nhanh",
                    DefinitionEn: "to move fast on foot",
                    Explanation: "Dùng cho người và động vật di chuyển nhanh",
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("She runs five miles every morning.", "Cô ấy chạy 5 dặm mỗi sáng.", null),
                        new("He had to run to catch the bus.", "Anh ấy phải chạy để kịp xe buýt.", null)
                    }
                ),
                new(
                    PartOfSpeech: PartOfSpeech.Verb,
                    DefinitionVi: "vận hành, quản lý doanh nghiệp hoặc hệ thống",
                    DefinitionEn: "to manage or be in charge of",
                    Explanation: "Thường dùng trong kinh doanh, quản trị",
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("They run a successful software company.", "Họ điều hành một công ty phần mềm thành công.", null)
                    }
                ),
                new(
                    PartOfSpeech: PartOfSpeech.Noun,
                    DefinitionVi: "chuyến đi, đợt chạy hoặc quãng đường",
                    DefinitionEn: "an act or period of running",
                    Explanation: "Ví dụ: go for a run",
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("I go for a run in the park.", "Tôi đi chạy bộ trong công viên.", null)
                    }
                )
            },
            TopicIds: new List<Guid>(),
            ExamTagIds: new List<Guid>()
        );

        var created = await _vocabService.CreateWordAsync(createDto);

        // Verification of Create
        created.Should().NotBeNull();
        created.Term.Should().Be("run");
        created.PrimaryPartOfSpeech.Should().Be(PartOfSpeech.Verb);
        created.HasMultiplePartsOfSpeech.Should().BeTrue();
        created.AllPartsOfSpeech.Should().ContainInOrder(PartOfSpeech.Verb, PartOfSpeech.Noun);
        created.Meanings.Should().HaveCount(3);
        created.Meanings.Where(m => m.PartOfSpeech == PartOfSpeech.Verb).Should().HaveCount(2);
        created.Meanings.Where(m => m.PartOfSpeech == PartOfSpeech.Noun).Should().HaveCount(1);
        created.PartsOfSpeechDisplay.Should().Be("v. • n.");

        // 2. EDIT WORD: Remove Noun, Add Adjective, change Primary to Adjective, update Verb meaning
        var updateDto = new WordCreateOrUpdateDto(
            Id: created.Id,
            Term: "run",
            PhoneticUk: "/rʌn/",
            PhoneticUs: "/rʌn/",
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B1,
            PrimaryPartOfSpeech: PartOfSpeech.Adjective,
            Notes: "Updated notes",
            Meanings: new List<WordMeaningCreateDto>
            {
                new(
                    PartOfSpeech: PartOfSpeech.Verb,
                    DefinitionVi: "chạy nhanh (đã cập nhật)",
                    DefinitionEn: "to move swiftly on foot",
                    Explanation: null,
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("Run fast!", "Chạy nhanh lên!", null)
                    }
                ),
                new(
                    PartOfSpeech: PartOfSpeech.Adjective,
                    DefinitionVi: "được đúc, chảy lỏng (như kim loại nóng chảy)",
                    DefinitionEn: "melted or in a liquid state",
                    Explanation: "Thuật ngữ kỹ thuật hoặc nấu chảy",
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("Run butter or run metal.", "Bơ chảy hoặc kim loại nóng chảy.", null)
                    }
                )
            },
            TopicIds: new List<Guid>(),
            ExamTagIds: new List<Guid>()
        );

        var updated = await _vocabService.UpdateWordAsync(updateDto);

        // Verification of Update
        updated.Should().NotBeNull();
        updated.Id.Should().Be(created.Id);
        updated.PrimaryPartOfSpeech.Should().Be(PartOfSpeech.Adjective);
        updated.Level.Should().Be(CefrLevel.B1);
        updated.Meanings.Should().HaveCount(2);
        updated.AllPartsOfSpeech.Should().ContainInOrder(PartOfSpeech.Adjective, PartOfSpeech.Verb);
        updated.AllPartsOfSpeech.Should().NotContain(PartOfSpeech.Noun);
        updated.PartsOfSpeechDisplay.Should().Be("adj. • v.");

        var fetched = await _vocabService.GetWordByIdAsync(created.Id);
        fetched.Should().NotBeNull();
        fetched!.PrimaryPartOfSpeech.Should().Be(PartOfSpeech.Adjective);
        fetched.Meanings.Should().HaveCount(2);
        fetched.AllPartsOfSpeech.Should().Contain(PartOfSpeech.Verb);
        fetched.AllPartsOfSpeech.Should().Contain(PartOfSpeech.Adjective);
        fetched.AllPartsOfSpeech.Should().NotContain(PartOfSpeech.Noun);
    }

    [Fact]
    public async Task CommitSmartImportAsync_WithConflictResolutionOverwrite_ShouldSuccessfullyUpdateExistingWordWithoutUniqueConstraintViolation()
    {
        // 1. Arrange: Create an initial word with topics, exam tags, and meanings
        var topic = new Domain.Entities.Topic { Name = "Business" };
        var exam = new Domain.Entities.ExamTag { Name = "IELTS", Type = ExamType.Ielts };
        _context.Topics.Add(topic);
        _context.ExamTags.Add(exam);
        await _context.SaveChangesAsync();

        var createDto = new WordCreateOrUpdateDto(
            Id: null,
            Term: "Shortlist",
            PhoneticUk: "/ˈʃɔːt.lɪst/",
            PhoneticUs: "/ˈʃɔːrt.lɪst/",
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B2,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: "Initial note",
            Meanings: new List<WordMeaningCreateDto>
            {
                new(
                    PartOfSpeech: PartOfSpeech.Noun,
                    DefinitionVi: "Danh sách rút gọn ban đầu",
                    DefinitionEn: "A list of candidates",
                    Explanation: "Initial explanation",
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("His name is on the shortlist.", "Tên anh ấy trong danh sách rút gọn.", null)
                    }
                )
            },
            TopicIds: new List<Guid> { topic.Id },
            ExamTagIds: new List<Guid> { exam.Id }
        );

        var created = await _vocabService.CreateWordAsync(createDto);
        created.Should().NotBeNull();

        // 2. Act: Prepare SmartImportItemDto matching the user's JSON with Overwrite conflict resolution
        var importItem = new SmartImportItemDto(
            RowIndex: 1,
            Term: "Shortlist",
            Level: CefrLevel.B2,
            PartOfSpeech: PartOfSpeech.Noun,
            DefinitionVi: "Danh sách sàng lọc, danh sách rút gọn",
            DefinitionEn: "A list of people who have been judged the most suitable",
            PhoneticUk: "/ˈʃɔːt.lɪst/",
            PhoneticUs: "/ˈʃɔːrt.lɪst/",
            Examples: "His name has been included on the final shortlist.",
            Topics: "Business",
            ExamTags: "IELTS, TOEIC",
            Notes: "Updated note",
            IsDuplicate: true,
            IsValid: true,
            ValidationMessage: null,
            IsSelected: true,
            RichMeanings: new List<WordMeaningCreateDto>
            {
                new(
                    PartOfSpeech: PartOfSpeech.Noun,
                    DefinitionVi: "Danh sách sàng lọc, danh sách rút gọn",
                    DefinitionEn: "A list of people who have been judged the most suitable for a job",
                    Explanation: "Danh sách các ứng viên sáng giá nhất",
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("His name has been included on the final shortlist for the branch manager role.", "Tên của anh ấy đã được đưa vào danh sách rút gọn.", null)
                    }
                ),
                new(
                    PartOfSpeech: PartOfSpeech.Verb,
                    DefinitionVi: "Sàng lọc, đưa vào danh sách rút gọn",
                    DefinitionEn: "To put someone on a shortlist",
                    Explanation: "Hành động chọn lọc ứng viên",
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("Candidates who are shortlisted will be contacted next week.", "Các ứng viên lọt vào danh sách rút gọn sẽ được liên hệ.", null)
                    }
                )
            },
            AllPartsOfSpeech: new List<PartOfSpeech> { PartOfSpeech.Noun, PartOfSpeech.Verb }
        );

        var exportImportService = new ExportImportService(_vocabRepo, _unitOfWork);
        var request = new SmartImportCommitRequestDto(
            Items: new List<SmartImportItemDto> { importItem },
            ConflictResolution: ImportConflictResolution.Overwrite
        );

        var result = await exportImportService.CommitSmartImportAsync(request);

        // 3. Assert: Verify import commit results
        result.TotalUpdated.Should().Be(1);
        result.TotalImported.Should().Be(0);
        result.TotalErrors.Should().Be(0);
        result.Errors.Should().BeEmpty();

        // Verify word in database
        var updatedWord = await _vocabRepo.GetByTermAsync("shortlist");
        updatedWord.Should().NotBeNull();
        updatedWord!.Meanings.Should().HaveCount(2);
        updatedWord.WordTopics.Should().HaveCount(1);
        updatedWord.WordExamTags.Should().HaveCount(2); // IELTS and newly created TOEIC

        // Test idempotency: Calling CommitSmartImportAsync again with Overwrite must succeed without errors
        var secondResult = await exportImportService.CommitSmartImportAsync(request);
        secondResult.TotalUpdated.Should().Be(1);
        secondResult.TotalErrors.Should().Be(0);
    }

    [Fact]
    public async Task TopicAndContextUnits_FullLifecycle_ShouldPreventDuplicatesAndOrphans()
    {
        // 1. Create 3 words
        var w1 = await _vocabService.CreateWordAsync(new WordCreateOrUpdateDto(
            null, "negotiate", null, null, null, null, CefrLevel.B2, PartOfSpeech.Verb, null,
            new List<WordMeaningCreateDto> { new(PartOfSpeech.Verb, "đàm phán, thương lượng", "to discuss something in order to reach an agreement", null, []) },
            [], []));

        var w2 = await _vocabService.CreateWordAsync(new WordCreateOrUpdateDto(
            null, "revenue", null, null, null, null, CefrLevel.B2, PartOfSpeech.Noun, null,
            new List<WordMeaningCreateDto> { new(PartOfSpeech.Noun, "doanh thu", "income that a company receives", null, []) },
            [], []));

        var w3 = await _vocabService.CreateWordAsync(new WordCreateOrUpdateDto(
            null, "contract", null, null, null, null, CefrLevel.B1, PartOfSpeech.Noun, null,
            new List<WordMeaningCreateDto> { new(PartOfSpeech.Noun, "hợp đồng", "an official written agreement", null, []) },
            [], []));

        // 2. Create Topic
        var topicDto = await _vocabService.CreateTopicAsync(new TopicCreateOrUpdateDto("Corporate Business", "Words for business contracts", "\uE825", "#0078D4"));
        var topicId = topicDto.Id;

        // 3. Assign w1, w2 to Topic
        await _vocabService.AssignWordsToTopicAsync(topicId, new[] { w1.Id, w2.Id });

        var units = await _vocabService.GetContextUnitsAsync();
        var corpUnit = units.FirstOrDefault(u => u.Id == topicId);
        corpUnit.Should().NotBeNull();
        corpUnit!.TotalWords.Should().Be(2);

        // 4. Test Duplicate Prevention: Re-assign w1, w2 along with w3
        await _vocabService.AssignWordsToTopicAsync(topicId, new[] { w1.Id, w2.Id, w3.Id });

        var unitsAfter = await _vocabService.GetContextUnitsAsync();
        corpUnit = unitsAfter.FirstOrDefault(u => u.Id == topicId);
        corpUnit!.TotalWords.Should().Be(3); // strictly 3, no duplicate WordTopic records!

        // 5. Update Topic
        var updatedTopic = await _vocabService.UpdateTopicAsync(topicId, new TopicCreateOrUpdateDto("International Business", "Updated desc", "\uE709", "#107C41"));
        updatedTopic.Name.Should().Be("International Business");

        // 6. Unassign w3 from Topic
        await _vocabService.UnassignWordsFromTopicAsync(topicId, new[] { w3.Id });
        unitsAfter = await _vocabService.GetContextUnitsAsync();
        corpUnit = unitsAfter.FirstOrDefault(u => u.Id == topicId);
        corpUnit!.TotalWords.Should().Be(2);

        // 7. Delete Topic & verify words still exist (no cascade deletion of words) and no orphan junction records
        await _vocabService.DeleteTopicAsync(topicId);

        var allWords = await _vocabRepo.SearchAsync(null, null, null, null, null, null, 0, 100);
        allWords.Should().Contain(w => w.Term == "negotiate");
        allWords.Should().Contain(w => w.Term == "revenue");
        allWords.Should().Contain(w => w.Term == "contract");

        // Verify no orphan WordTopics
        var orphanJunctions = await _context.WordTopics.Where(wt => wt.TopicId == topicId).ToListAsync();
        orphanJunctions.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ByVietnameseMeaning_AccentedAndUnaccented_Test()
    {
        var word = new Word
        {
            Term = "collaborate",
            NormalizedTerm = "collaborate",
            Level = CefrLevel.B2,
            PrimaryPartOfSpeech = PartOfSpeech.Verb,
            Meanings = new List<WordMeaning>
            {
                new()
                {
                    DefinitionVi = "hợp tác, cộng tác",
                    PartOfSpeech = PartOfSpeech.Verb
                }
            }
        };
        await _vocabRepo.AddAsync(word);
        await _unitOfWork.SaveChangesAsync();

        var resAccented = await _vocabRepo.SearchAsync("hợp tác", null, null, null, null, null, 0, 10);
        var resUnaccented = await _vocabRepo.SearchAsync("hop tac", null, null, null, null, null, 0, 10);

        resAccented.Should().NotBeEmpty();
        resUnaccented.Should().NotBeEmpty();

        var serviceAccented = await _vocabService.SearchWordsAsync(search: "hợp tác");
        var serviceUnaccented = await _vocabService.SearchWordsAsync(search: "hop tac");
        var countUnaccented = await _vocabService.CountWordsAsync(search: "hop tac");

        serviceAccented.Should().NotBeEmpty();
        serviceUnaccented.Should().NotBeEmpty();
        countUnaccented.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BulkDelete_WithChunkingAndLargeBatch_ShouldDeleteAllAndCleanRelations()
    {
        // 1. Create 320 words in batches
        var ids = new List<Guid>();
        for (int i = 0; i < 320; i++)
        {
            var word = new Word
            {
                Term = $"batchword_{i}",
                NormalizedTerm = $"batchword_{i}",
                Level = CefrLevel.A1,
                PrimaryPartOfSpeech = PartOfSpeech.Noun,
                Meanings = new List<WordMeaning>
                {
                    new()
                    {
                        PartOfSpeech = PartOfSpeech.Noun,
                        DefinitionVi = $"nghĩa của từ {i}",
                        DefinitionEn = $"meaning of word {i}"
                    }
                }
            };
            await _context.Words.AddAsync(word);
            ids.Add(word.Id);
        }
        await _context.SaveChangesAsync();

        var reports = new List<ProgressReport>();
        var progress = new Progress<ProgressReport>(r => reports.Add(r));

        // 2. Act: Delete in chunks (150 chunk size tested across 320 items)
        int deleted = await _vocabService.DeleteWordsAsync(ids, CancellationToken.None, progress);

        // 3. Assert
        deleted.Should().Be(320);
        var remaining = await _context.Words.Where(w => ids.Contains(w.Id)).CountAsync();
        remaining.Should().Be(0);

        var remainingMeanings = await _context.WordMeanings.Where(wm => ids.Contains(wm.WordId)).CountAsync();
        remainingMeanings.Should().Be(0);
    }

    [Fact]
    public async Task ExportVocabularyOnly_ShouldGeneratePureTermAndMeaningsWithoutGrammarOrLogs()
    {
        // 1. Create word
        var word = await _vocabService.CreateWordAsync(new WordCreateOrUpdateDto(
            null, "ubiquitous", "/juːˈbɪk.wɪ.təs/", null, null, null, CefrLevel.C1, PartOfSpeech.Adjective, "Everywhere",
            new List<WordMeaningCreateDto>
            {
                new(PartOfSpeech.Adjective, "có mặt ở khắp nơi, phổ biến", "present, appearing, or found everywhere", "formal context", [])
            },
            [], []));

        var exportImportService = new ExportImportService(_vocabRepo, _unitOfWork);

        // 2. Export JSON Vocab Only
        var json = await exportImportService.ExportVocabularyOnlyJsonAsync(new[] { word.Id });
        json.Should().Contain("\"term\": \"ubiquitous\"");
        json.Should().Contain("\"có mặt ở khắp nơi, phổ biến\"");
        json.Should().NotContain("studyLogs");
        json.Should().NotContain("grammarRules");

        // 3. Export CSV Vocab Only
        var csv = await exportImportService.ExportVocabularyOnlyCsvAsync(new[] { word.Id });
        csv.Should().StartWith("Term,DefinitionVi,DefinitionEn,PartOfSpeech");
        csv.Should().Contain("ubiquitous");
        csv.Should().Contain("\"có mặt ở khắp nơi, phổ biến\"");

        // 4. Export TXT Vocab Only
        var txt = await exportImportService.ExportVocabularyOnlyTxtAsync(new[] { word.Id });
        txt.Should().Contain("ubiquitous\tcó mặt ở khắp nơi, phổ biến");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
