using System.Data;
using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Application.Services.Phrases;
using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;
using Lexora.Infrastructure.Data;
using Lexora.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexora.Tests.Integration;

public class ConversationPhraseUpgradeIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LexoraDbContext _context;
    private readonly ConversationPhraseRepository _phraseRepo;
    private readonly VocabularyRepository _vocabRepo;
    private readonly ReviewRepository _reviewRepo;
    private readonly UnitOfWork _unitOfWork;
    private readonly SqliteDatabaseInitializer _initializer;

    public ConversationPhraseUpgradeIntegrationTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LexoraDbContext(options);
        _initializer = new SqliteDatabaseInitializer(_context, NullLogger<SqliteDatabaseInitializer>.Instance);
        _initializer.InitializeAsync().GetAwaiter().GetResult();

        _phraseRepo = new ConversationPhraseRepository(_context);
        _vocabRepo = new VocabularyRepository(_context);
        _reviewRepo = new ReviewRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    private ConversationPhraseService CreateService() => new(
        _phraseRepo,
        _unitOfWork,
        fuzzySearchService: new FuzzySearchService(),
        reviewRepository: _reviewRepo,
        vocabularyRepository: _vocabRepo);

    [Fact]
    public async Task Initializer_ShouldCreateExtendedColumnsAndCompositeIndexes()
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = """PRAGMA table_info("ConversationPhrases");""";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(1));
            }
        }

        columns.Should().Contain(["Register", "ExamTarget", "KeyPhrases", "FormalVariation", "CasualVariation", "CommonMistakesVi", "Status", "EaseFactor", "NextReviewAt", "LinkedWordId"]);

        var indexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var idxCmd = _connection.CreateCommand())
        {
            idxCmd.CommandText = """SELECT name FROM sqlite_master WHERE type = 'index' AND tbl_name = 'ConversationPhrases';""";
            await using var reader = await idxCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                indexes.Add(reader.GetString(0));
            }
        }

        indexes.Should().Contain(["IX_ConversationPhrases_Register", "IX_ConversationPhrases_ExamTarget", "IX_ConversationPhrases_Status", "IX_ConversationPhrases_NextReviewAt", "IX_ConversationPhrases_Category_CefrLevel_Register"]);
    }

    [Fact]
    public async Task Initializer_OnLegacyConversationPhrasesTable_ShouldMigrateColumnsAndPreserveRows()
    {
        // Arrange: separate in-memory DB simulating pre-upgrade 14-column schema
        await using var legacyConn = new SqliteConnection("Filename=:memory:");
        await legacyConn.OpenAsync();

        var legacyOptions = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(legacyConn)
            .Options;

        await using var legacyCtx = new LexoraDbContext(legacyOptions);
        await legacyCtx.Database.EnsureCreatedAsync();
        await legacyCtx.Database.ExecuteSqlRawAsync("""DROP TABLE "ConversationPhrases";""");

        const string legacyTableSql = """
            CREATE TABLE "ConversationPhrases" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ConversationPhrases" PRIMARY KEY,
                "Category" TEXT NOT NULL,
                "ContextTag" TEXT NOT NULL DEFAULT '',
                "PromptSentence" TEXT NOT NULL,
                "PromptTranslation" TEXT NOT NULL,
                "SampleResponse" TEXT NOT NULL,
                "ResponseTranslation" TEXT NOT NULL,
                "AdditionalSuggestions" TEXT NOT NULL DEFAULT '',
                "CefrLevel" INTEGER NOT NULL DEFAULT 3,
                "IsFavorite" INTEGER NOT NULL DEFAULT 0,
                "NormalizedPrompt" TEXT NOT NULL DEFAULT '',
                "NormalizedResponse" TEXT NOT NULL DEFAULT '',
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            INSERT INTO "ConversationPhrases"
            ("Id", "Category", "ContextTag", "PromptSentence", "PromptTranslation", "SampleResponse", "ResponseTranslation", "AdditionalSuggestions", "CefrLevel", "IsFavorite", "NormalizedPrompt", "NormalizedResponse", "CreatedAt", "UpdatedAt")
            VALUES
            ('11111111-1111-1111-1111-111111111111', 'Workplace & Office', 'LegacyTag', 'Legacy prompt?', 'Hoi cu?', 'Legacy response.', 'Dap cu.', '', 3, 1, 'legacy prompt?', 'legacy response.', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z');
            """;
        await legacyCtx.Database.ExecuteSqlRawAsync(legacyTableSql);

        // Act: run full initializer on legacy DB
        var migrator = new SqliteDatabaseInitializer(legacyCtx, NullLogger<SqliteDatabaseInitializer>.Instance);
        await migrator.InitializeAsync();

        // Assert: legacy row survived and new defaults populated
        var migrated = await legacyCtx.ConversationPhrases.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse("11111111-1111-1111-1111-111111111111"));

        migrated.Should().NotBeNull();
        migrated!.PromptSentence.Should().Be("Legacy prompt?");
        migrated.Register.Should().Be(PhraseRegister.Neutral);
        migrated.EaseFactor.Should().Be(2.5);
    }

    [Fact]
    public async Task Repository_GetFilteredAdvancedAsync_And_DueQueries_ShouldWorkWithSqlite()
    {
        var duePhrase = new ConversationPhrase
        {
            Category = "Business & TOEIC",
            ContextTag = "Budget",
            CefrLevel = CefrLevel.B2,
            Register = PhraseRegister.Formal,
            ExamTarget = ExamTarget.Toeic,
            PromptSentence = "Could we review the budget breakdown?",
            PromptTranslation = "Chung ta xem phan bo ngan sach duoc khong?",
            SampleResponse = "Certainly, let's check the spreadsheet.",
            ResponseTranslation = "Chac chan roi.",
            KeyPhrases = "budget breakdown; spreadsheet",
            IsFavorite = true,
            NextReviewAt = DateTime.UtcNow.AddDays(-1)
        };
        duePhrase.UpdateNormalizedFields();

        var futurePhrase = new ConversationPhrase
        {
            Category = "Travel & Airport",
            CefrLevel = CefrLevel.A2,
            Register = PhraseRegister.Casual,
            ExamTarget = null,
            PromptSentence = "Where is gate 12?",
            PromptTranslation = "Cong 12 o dau?",
            SampleResponse = "Just down the hall on your left.",
            ResponseTranslation = "Di thang cuoi hanh lang ben trai.",
            Status = LearningStatus.Remembered,
            NextReviewAt = DateTime.UtcNow.AddDays(5)
        };
        futurePhrase.UpdateNormalizedFields();

        await _phraseRepo.AddAsync(duePhrase);
        await _phraseRepo.AddAsync(futurePhrase);
        await _unitOfWork.SaveChangesAsync();

        var filtered = await _phraseRepo.GetFilteredAdvancedAsync(
            category: "Business",
            level: CefrLevel.B2,
            register: PhraseRegister.Formal,
            examTarget: ExamTarget.Toeic,
            onlyFavorites: true,
            onlyDueForReview: true,
            search: "spreadsheet");

        filtered.Should().ContainSingle(p => p.Id == duePhrase.Id);

        var byPrompt = await _phraseRepo.GetByNormalizedPromptAsync("  COULD WE REVIEW THE BUDGET BREAKDOWN? ");
        byPrompt.Should().NotBeNull();
        byPrompt!.Id.Should().Be(duePhrase.Id);

        var dueList = await _phraseRepo.GetDuePhrasesAsync(DateTime.UtcNow, limit: 10);
        dueList.Should().Contain(p => p.Id == duePhrase.Id);
        dueList.Should().NotContain(p => p.Id == futurePhrase.Id);
    }

    [Fact]
    public async Task Service_RoleplayReviewAndImportExport_ShouldPersistEndToEndInSqlite()
    {
        var sut = CreateService();

        var created = await sut.CreateAsync(new CreateConversationPhraseDto
        {
            Category = "Academic & IELTS Speaking",
            ContextTag = "Part 3 Opinion",
            CefrLevel = CefrLevel.C1,
            Register = PhraseRegister.Formal,
            ExamTarget = ExamTarget.Ielts,
            PromptSentence = "How does urbanization impact community bonds?",
            PromptTranslation = "Do thi hoa tac dong the nao den su gan ket cong dong?",
            SampleResponse = "To a certain extent, fast-paced living can weaken neighborhood ties.",
            ResponseTranslation = "O mot muc do nao do, nhip song nhanh co the lam suy yeu tinh lang nghia xom.",
            KeyPhrases = "To a certain extent; fast-paced living; neighborhood ties",
            FormalVariation = "Rapid urban expansion often attenuates traditional communal bonds.",
            CasualVariation = "City life gets pretty busy, so neighbors barely talk nowadays.",
            CommonMistakesVi = "Tranh lap 'In my opinion, I think...' trong IELTS Speaking."
        });

        // 1. Roleplay queue & SM-2 review persistence
        var queue = await sut.GetRoleplayQueueAsync(new ConversationPhraseFilterDto { ExamTarget = ExamTarget.Ielts });
        queue.Should().Contain(c => c.Phrase.Id == created.Id);

        var reviewResult = await sut.SubmitPhraseReviewAsync(created.Id, ReviewRating.Good, responseTimeMs: 1400);
        reviewResult.SyncedStudyLog.Should().BeTrue();
        reviewResult.NewEaseFactor.Should().BeGreaterThanOrEqualTo(1.3);

        var logs = await _context.StudyLogs.AsNoTracking().ToListAsync();
        logs.Should().NotBeEmpty();

        // 2. JSON & CSV roundtrip + deduplication
        var jsonExport = await sut.ExportToJsonAsync([created.Id]);
        var csvExport = await sut.ExportToCsvAsync([created.Id]);

        var dupSkip = await sut.ImportSmartAsync(csvExport, "export.csv", ImportConflictResolution.Skip);
        dupSkip.TotalImported.Should().Be(0);
        dupSkip.TotalSkipped.Should().Be(1);

        var dupOverwrite = await sut.ImportSmartAsync(jsonExport, "export.json", ImportConflictResolution.Overwrite);
        dupOverwrite.TotalUpdated.Should().Be(1);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
