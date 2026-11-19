using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Application.Services.Search;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Lexora.Infrastructure.Data.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class LexoraV2UpgradeTests
{
    [Fact]
    public void Fsrs5_SameDayReview_UsesShortTermStabilityAndUpdatesState()
    {
        var engine = new FsrsSpacedRepetitionEngine();
        var now = DateTime.UtcNow;
        var card = new ReviewCard
        {
            WordId = Guid.NewGuid(),
            Status = LearningStatus.Learning,
            Stability = 3.17,
            Difficulty = 5.0,
            Repetitions = 1,
            LastReviewedAt = now.AddHours(-2)
        };

        var goodState = engine.CalculateFsrsNextReview(card, ReviewRating.Good, now);
        var easyState = engine.CalculateFsrsNextReview(card, ReviewRating.Easy, now);

        goodState.Stability.Should().BeGreaterThan(card.Stability);
        easyState.Stability.Should().BeGreaterThan(goodState.Stability);
        goodState.Repetitions.Should().Be(2);
    }

    [Fact]
    public void Fsrs5_LeechGuard_DetectsChronicLapsesAndBoostsPriority()
    {
        var engine = new FsrsSpacedRepetitionEngine();
        var now = DateTime.UtcNow;

        var normalCard = new ReviewCard
        {
            Status = LearningStatus.Remembered,
            Stability = 10.0,
            Repetitions = 3,
            Lapses = 1,
            TotalReviews = 5,
            CorrectReviews = 4,
            LastReviewedAt = now.AddDays(-5),
            NextReviewAt = now
        };

        var leechCard = new ReviewCard
        {
            Status = LearningStatus.NotRemembered,
            Stability = 1.5,
            Repetitions = 1,
            Lapses = 6,
            TotalReviews = 10,
            CorrectReviews = 3,
            LastReviewedAt = now.AddDays(-5),
            NextReviewAt = now
        };

        engine.IsLeechCard(normalCard).Should().BeFalse();
        engine.IsLeechCard(leechCard).Should().BeTrue();
        engine.CalculatePriorityScore(leechCard, now)
            .Should().BeGreaterThan(engine.CalculatePriorityScore(normalCard, now));
    }

    [Fact]
    public void PhoneticEncoder_MatchesSoundAlikeMisspelling()
    {
        double sim = PhoneticEncoder.CalculateSimilarity("nolidge", "knowledge");
        sim.Should().BeGreaterThanOrEqualTo(0.75);

        var search = new SmartSearchService();
        var word = new WordDto(
            Id: Guid.NewGuid(),
            Term: "knowledge",
            PhoneticUk: "/ˈnɒl.ɪdʒ/",
            PhoneticUs: "/ˈnɑː.lɪdʒ/",
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B1,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: null,
            Meanings: new List<WordMeaningDto>
            {
                new(Guid.NewGuid(), PartOfSpeech.Noun, "kiến thức", "understanding", null, 0, new List<ExampleSentenceDto>())
            },
            Topics: new List<string>(),
            ExamTags: new List<string>(),
            LearningStatus: LearningStatus.New,
            NextReviewAt: null,
            Confidence: 0,
            IntervalDays: 0,
            EaseFactor: 2.5,
            Lapses: 0,
            Repetitions: 0,
            TotalReviews: 0,
            RetentionRate: 100
        );

        var match = search.ScoreWord("nolidge", word);
        match.Should().NotBeNull();
        match!.MatchType.Should().BeOneOf("Phonetic", "Fuzzy");
    }

    [Fact]
    public async Task AdaptiveQuiz_IrtEngine_UpdatesContinuousAbilityTheta()
    {
        var vocabRepo = new Mock<IVocabularyRepository>();
        var reviewRepo = new Mock<IReviewRepository>();
        var uow = new Mock<IUnitOfWork>();

        var words = Enumerable.Range(1, 8).Select(i => new Word
        {
            Id = Guid.NewGuid(),
            Term = $"word{i}",
            NormalizedTerm = $"word{i}",
            Level = i <= 4 ? CefrLevel.B1 : CefrLevel.B2,
            PrimaryPartOfSpeech = PartOfSpeech.Noun,
            Meanings = new List<WordMeaning>
            {
                new() { DefinitionVi = $"nghĩa {i}", PartOfSpeech = PartOfSpeech.Noun }
            }
        }).ToList();

        vocabRepo.Setup(r => r.SearchAsync(
                It.IsAny<string?>(), It.IsAny<CefrLevel?>(), It.IsAny<PartOfSpeech?>(),
                It.IsAny<Guid?>(), It.IsAny<ExamType?>(), It.IsAny<LearningStatus?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(words);

        vocabRepo.Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word>());

        var service = new AdaptiveQuizService(vocabRepo.Object, reviewRepo.Object, uow.Object);
        var q1 = await service.StartQuizAsync(new AdaptiveQuizConfigDto(QuestionCount: 2, InitialLevel: CefrLevel.B1, FocusWeakWords: false));

        q1.Should().NotBeNull();
        var eval1 = await service.SubmitAnswerAsync(q1!.SessionId, q1.QuestionIndex, q1.CorrectOptionIndex, 2000);
        eval1.IsCorrect.Should().BeTrue();
        eval1.EstimatedAbilityTheta.Should().BeGreaterThan(0.0);

        var summary = await service.FinishQuizAsync(q1.SessionId);
        summary.FinalAbilityTheta.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public async Task VersionedSqliteMigrator_UpgradesV0Database_NonDestructively()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // Create minimal legacy v0 ReviewCards table without v2 columns
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE "ReviewCards" (
                    "Id" TEXT NOT NULL PRIMARY KEY,
                    "WordId" TEXT NOT NULL,
                    "Status" INTEGER NOT NULL,
                    "EaseFactor" REAL NOT NULL,
                    "Stability" REAL NOT NULL DEFAULT 0.0,
                    "Difficulty" REAL NOT NULL DEFAULT 5.0,
                    "IntervalDays" INTEGER NOT NULL,
                    "Repetitions" INTEGER NOT NULL,
                    "Lapses" INTEGER NOT NULL,
                    "LastReviewedAt" TEXT NULL,
                    "NextReviewAt" TEXT NOT NULL,
                    "TotalReviews" INTEGER NOT NULL,
                    "CorrectReviews" INTEGER NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NULL
                );
                INSERT INTO "ReviewCards" ("Id", "WordId", "Status", "EaseFactor", "IntervalDays", "Repetitions", "Lapses", "NextReviewAt", "TotalReviews", "CorrectReviews", "CreatedAt")
                VALUES ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 3, 2.5, 6, 2, 6, '2026-11-20T00:00:00Z', 10, 3, '2026-11-01T00:00:00Z');
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new LexoraDbContext(options);
        await VersionedSqliteMigrator.MigrateToLatestAsync(db, NullLogger.Instance);

        using var verifyCmd = connection.CreateCommand();
        verifyCmd.CommandText = "PRAGMA user_version;";
        var version = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());
        version.Should().Be(VersionedSqliteMigrator.CurrentSchemaVersion);

        verifyCmd.CommandText = "SELECT \"IsLeech\" FROM \"ReviewCards\" WHERE \"Id\" = '11111111-1111-1111-1111-111111111111';";
        var isLeech = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());
        isLeech.Should().Be(1);

        verifyCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ItemCalibrations';";
        var tableExists = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());
        tableExists.Should().Be(1);
    }
}
