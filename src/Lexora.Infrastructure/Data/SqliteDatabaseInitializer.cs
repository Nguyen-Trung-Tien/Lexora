using Lexora.Domain.Entities;
using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.Data;

public class SqliteDatabaseInitializer
{
    private readonly LexoraDbContext _context;
    private readonly ILogger<SqliteDatabaseInitializer> _logger;

    public SqliteDatabaseInitializer(LexoraDbContext context, ILogger<SqliteDatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        int attempt = 0;

        while (true)
        {
            attempt++;
            try
            {
                _logger.LogInformation("Ensuring SQLite database is created (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
                await _context.Database.EnsureCreatedAsync(cancellationToken);

                // Configure optimal client SQLite PRAGMAs for high performance and lock resilience
                await _context.Database.ExecuteSqlRawAsync(
                    "PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL; PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 15000; PRAGMA cache_size = -8000; PRAGMA temp_store = MEMORY; PRAGMA mmap_size = 30000000;",
                    cancellationToken);

                await EnsureSchemaCompatibilityAsync(cancellationToken);

                if (!await _context.Words.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Seeding initial starter pack data...");
                    await SeedInitialDataAsync(cancellationToken);
                }

                if (!await _context.HomophoneGroups.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Seeding starter homophones and phonetic confusions...");
                    await SeedHomophoneStarterDataAsync(cancellationToken);
                }

                if (!await _context.ConversationPhrases.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Seeding starter conversation phrases and common scenarios...");
                    await SeedPhraseStarterDataAsync(cancellationToken);
                }

                _logger.LogInformation("SQLite database initialized successfully.");
                break;
            }
            catch (SqliteException ex) when (attempt < maxRetries && (ex.SqliteErrorCode == 5 || ex.Message.Contains("busy", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning(ex, "SQLite database busy/locked on initialization attempt {Attempt}/{MaxRetries}. Retrying after backoff...", attempt, maxRetries);
                SqliteConnection.ClearAllPools();
                await Task.Delay(250 * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the SQLite database on attempt {Attempt}.", attempt);
                throw;
            }
        }
    }

    private async Task SeedInitialDataAsync(CancellationToken ct)
    {
        // 1. Topics (reuse existing if present)
        var existingTopics = await _context.Topics.ToListAsync(ct);
        var businessTopic = existingTopics.FirstOrDefault(t => t.Name.Equals("Business & Career", StringComparison.OrdinalIgnoreCase));
        if (businessTopic == null)
        {
            businessTopic = new Topic
            {
                Name = "Business & Career",
                Description = "Vocabularies for office, meetings, and business communication",
                IconGlyph = "\uE825",
                ColorHex = "#2B579A"
            };
            await _context.Topics.AddAsync(businessTopic, ct);
        }

        var techTopic = existingTopics.FirstOrDefault(t => t.Name.Equals("Technology", StringComparison.OrdinalIgnoreCase));
        if (techTopic == null)
        {
            techTopic = new Topic
            {
                Name = "Technology",
                Description = "Software, computers, digital innovation",
                IconGlyph = "\uE770",
                ColorHex = "#0078D4"
            };
            await _context.Topics.AddAsync(techTopic, ct);
        }

        var dailyTopic = existingTopics.FirstOrDefault(t => t.Name.Equals("Daily Life", StringComparison.OrdinalIgnoreCase));
        if (dailyTopic == null)
        {
            dailyTopic = new Topic
            {
                Name = "Daily Life",
                Description = "Everyday conversation, routines, hobbies",
                IconGlyph = "\uE80F",
                ColorHex = "#107C41"
            };
            await _context.Topics.AddAsync(dailyTopic, ct);
        }

        // 2. Exam Tags (reuse existing if present)
        var existingTags = await _context.ExamTags.ToListAsync(ct);
        var toeicTag = existingTags.FirstOrDefault(et => et.Name.Equals("TOEIC 650+", StringComparison.OrdinalIgnoreCase));
        if (toeicTag == null)
        {
            toeicTag = new ExamTag { Name = "TOEIC 650+", Type = ExamType.Toeic, TargetScore = "650" };
            await _context.ExamTags.AddAsync(toeicTag, ct);
        }

        var vstepTag = existingTags.FirstOrDefault(et => et.Name.Equals("VSTEP B2", StringComparison.OrdinalIgnoreCase));
        if (vstepTag == null)
        {
            vstepTag = new ExamTag { Name = "VSTEP B2", Type = ExamType.Vstep, TargetScore = "B2" };
            await _context.ExamTags.AddAsync(vstepTag, ct);
        }

        var ieltsTag = existingTags.FirstOrDefault(et => et.Name.Equals("IELTS 6.5", StringComparison.OrdinalIgnoreCase));
        if (ieltsTag == null)
        {
            ieltsTag = new ExamTag { Name = "IELTS 6.5", Type = ExamType.Ielts, TargetScore = "6.5" };
            await _context.ExamTags.AddAsync(ieltsTag, ct);
        }

        // 3. Words Starter Pack
        var sampleWords = new List<Word>
        {
            CreateWord("collaborate", "kəˈlæb.ə.reɪt", "kəˈlæb.ə.reɪt", CefrLevel.B2, PartOfSpeech.Verb,
                "To work together with someone for a special purpose",
                "hợp tác, cộng tác",
                "Two teams collaborated on the research project.",
                "Hai nhóm đã hợp tác cùng nhau trong dự án nghiên cứu.",
                businessTopic, toeicTag),

            CreateWord("innovative", "ˈɪn.ə.veɪ.tɪv", "ˈɪn.oʊ.veɪ.tɪv", CefrLevel.B2, PartOfSpeech.Adjective,
                "Using new methods or ideas; advanced and original",
                "sáng tạo, đổi mới",
                "She came up with an innovative solution to the technical problem.",
                "Cô ấy đã đưa ra một giải pháp sáng tạo cho vấn đề kỹ thuật.",
                techTopic, toeicTag),

            CreateWord("negotiate", "nəˈɡəʊ.ʃi.eɪt", "nəˈɡoʊ.ʃi.eɪt", CefrLevel.B2, PartOfSpeech.Verb,
                "To have formal discussions with someone in order to reach an agreement",
                "thương lượng, đàm phán",
                "The government will not negotiate with the terrorists.",
                "Chính phủ sẽ không thương lượng với khủng bố.",
                businessTopic, toeicTag),

            CreateWord("resilient", "rɪˈzɪl.jənt", "rɪˈzɪl.jənt", CefrLevel.C1, PartOfSpeech.Adjective,
                "Able to be happy or successful again after something difficult has happened",
                "kiên cường, có khả năng phục hồi nhanh",
                "Optimists are more resilient in the face of setbacks.",
                "Những người lạc quan kiên cường hơn khi đối mặt với thất bại.",
                dailyTopic, vstepTag),

            CreateWord("crucial", "ˈkruː.ʃəl", "ˈkruː.ʃəl", CefrLevel.B2, PartOfSpeech.Adjective,
                "Extremely important or necessary",
                "cốt yếu, tối quan trọng",
                "Regular exercise is crucial for maintaining good health.",
                "Tập thể dục thường xuyên là điều cốt yếu để duy trì sức khỏe tốt.",
                dailyTopic, vstepTag),

            CreateWord("ubiquitous", "juːˈbɪk.wɪ.təs", "juːˈbɪk.wə.t̬əs", CefrLevel.C1, PartOfSpeech.Adjective,
                "Seeming to be everywhere",
                "phổ biến khắp nơi, nhan nhản",
                "Smartphones have become ubiquitous in modern society.",
                "Điện thoại thông minh đã trở nên phổ biến khắp nơi trong xã hội hiện đại.",
                techTopic, ieltsTag),

            CreateWord("allocate", "ˈæl.ə.keɪt", "ˈæl.ə.keɪt", CefrLevel.B2, PartOfSpeech.Verb,
                "To give something to someone as their share of a total amount",
                "phân bổ, cấp phát",
                "The company allocated funds for the new marketing campaign.",
                "Công ty đã phân bổ kinh phí cho chiến dịch tiếp thị mới.",
                businessTopic, toeicTag),

            CreateWord("meticulous", "məˈtɪk.jə.ləs", "məˈtɪk.jə.ləs", CefrLevel.C1, PartOfSpeech.Adjective,
                "Very careful and with great attention to every detail",
                "tỉ mỉ, kỹ lưỡng, cẩn thận từng chi tiết",
                "He kept meticulous records of all business transactions.",
                "Anh ấy lưu giữ hồ sơ tỉ mỉ của tất cả các giao dịch kinh doanh.",
                businessTopic, vstepTag),

            CreateWord("streamline", "ˈstriːm.laɪn", "ˈstriːm.laɪn", CefrLevel.C1, PartOfSpeech.Verb,
                "To make a system or organization work more efficiently",
                "hợp lý hóa, tinh giản quy trình",
                "The new software helps streamline company operations.",
                "Phần mềm mới giúp tinh giản các hoạt động của công ty.",
                techTopic, toeicTag),

            CreateWord("perseverance", "ˌpɜː.sɪˈvɪə.rəns", "ˌpɝː.səˈvɪr.əns", CefrLevel.B2, PartOfSpeech.Noun,
                "Continued effort to do or achieve something despite difficulties",
                "sự kiên trì, bền chí",
                "Through perseverance and hard work, he accomplished his dream.",
                "Nhờ sự kiên trì và làm việc chăm chỉ, anh ấy đã hoàn thành ước mơ.",
                dailyTopic, vstepTag)
        };

        await _context.Words.AddRangeAsync(sampleWords, ct);

        // 4. Grammar Rules Starter Pack
        var grammarRules = new List<GrammarRule>
        {
            new GrammarRule
            {
                Title = "Present Perfect vs. Past Simple",
                Structure = "S + have/has + V3/ed (Present Perfect) vs. S + V2/ed (Past Simple)",
                ExplanationVi = "Hiện tại hoàn thành dùng cho hành động bắt đầu trong quá khứ và còn liên hệ tới hiện tại, hoặc vừa mới xảy ra không có mốc thời gian cụ thể. Quá khứ đơn dùng khi hành động đã chấm dứt hoàn toàn tại một thời điểm xác định trong quá khứ.",
                ExplanationEn = "Present Perfect connects past actions to the present, whereas Past Simple describes actions completed at a specific past time.",
                CommonMistakes = "Dùng nhầm Present Perfect với các trạng từ chỉ thời gian cụ thể trong quá khứ như 'yesterday', 'in 2020', 'two days ago'.",
                Level = CefrLevel.B1,
                ExamTarget = ExamType.Toeic,
                Topic = "Tenses",
                OrderIndex = 1,
                Examples = new List<GrammarExample>
                {
                    new GrammarExample
                    {
                        SentenceEn = "I have lived in Hanoi for five years. (I still live here)",
                        SentenceVi = "Tôi đã sống ở Hà Nội được 5 năm. (Hiện tại vẫn đang sống ở đây)",
                        Note = "Present Perfect với 'for'"
                    },
                    new GrammarExample
                    {
                        SentenceEn = "I lived in Da Nang in 2018. (I don't live there anymore)",
                        SentenceVi = "Tôi đã sống ở Đà Nẵng vào năm 2018. (Giờ không còn ở đó nữa)",
                        Note = "Past Simple với mốc thời gian 'in 2018'"
                    }
                }
            },
            new GrammarRule
            {
                Title = "Conditional Type 2 (Unreal Present)",
                Structure = "If + S + V2/past subjunctive, S + would/could + V-bare",
                ExplanationVi = "Câu điều kiện loại 2 diễn tả giả định trái ngược với thực tế ở hiện tại hoặc tương lai.",
                ExplanationEn = "Second conditional expresses hypothetical or imaginary situations contrary to the present reality.",
                CommonMistakes = "Dùng 'will' thay cho 'would' trong mệnh đề chính, hoặc dùng 'was' thay vì 'were' trong văn phong học thuật chuẩn.",
                Level = CefrLevel.B1,
                ExamTarget = ExamType.Vstep,
                Topic = "Conditionals",
                OrderIndex = 2,
                Examples = new List<GrammarExample>
                {
                    new GrammarExample
                    {
                        SentenceEn = "If I had more free time, I would learn a third language.",
                        SentenceVi = "Nếu tôi có nhiều thời gian rảnh hơn, tôi sẽ học thêm một ngôn ngữ thứ ba.",
                        Note = "Thực tế hiện tại: Tôi không có nhiều thời gian rảnh."
                    }
                }
            }
        };

        await _context.GrammarRules.AddRangeAsync(grammarRules, ct);

        // 5. Initial Streak
        await _context.UserStreaks.AddAsync(new UserStreak
        {
            CurrentStreak = 1,
            LongestStreak = 1,
            LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalDaysLearned = 1
        }, ct);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Database seeded successfully with {Count} starter words.", sampleWords.Count);
    }

    private static Word CreateWord(
        string term,
        string ukPhonetic,
        string usPhonetic,
        CefrLevel level,
        PartOfSpeech pos,
        string defEn,
        string defVi,
        string exEn,
        string exVi,
        Topic topic,
        ExamTag examTag)
    {
        var word = new Word
        {
            Term = term,
            NormalizedTerm = term.ToLowerInvariant(),
            PhoneticUk = $"/{ukPhonetic}/",
            PhoneticUs = $"/{usPhonetic}/",
            Level = level,
            PrimaryPartOfSpeech = pos
        };

        var meaning = new WordMeaning
        {
            WordId = word.Id,
            PartOfSpeech = pos,
            DefinitionVi = defVi,
            DefinitionEn = defEn,
            OrderIndex = 0
        };

        meaning.Examples.Add(new ExampleSentence
        {
            WordMeaningId = meaning.Id,
            SentenceEn = exEn,
            SentenceVi = exVi
        });

        word.Meanings.Add(meaning);
        word.WordTopics.Add(new WordTopic { WordId = word.Id, Topic = topic });
        word.WordExamTags.Add(new WordExamTag { WordId = word.Id, ExamTag = examTag });

        word.ReviewCard = new ReviewCard
        {
            WordId = word.Id,
            Status = LearningStatus.New,
            NextReviewAt = DateTime.UtcNow
        };

        return word;
    }

    private async Task EnsureSchemaCompatibilityAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(\"StudyLogs\");";
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            bool needsStudySessionIdMigration = false;
            while (await reader.ReadAsync(cancellationToken))
            {
                var colName = reader.GetString(1);
                var notNull = reader.GetInt32(3);
                if (string.Equals(colName, "StudySessionId", StringComparison.OrdinalIgnoreCase) && notNull == 1)
                {
                    needsStudySessionIdMigration = true;
                    break;
                }
            }
            reader.Close();

            if (needsStudySessionIdMigration)
            {
                _logger.LogInformation("Migrating StudyLogs table to allow NULL StudySessionId...");
                const string migrationSql = """
                    PRAGMA foreign_keys = OFF;
                    CREATE TABLE "StudyLogs_temp" (
                        "Id" TEXT NOT NULL CONSTRAINT "PK_StudyLogs" PRIMARY KEY,
                        "StudySessionId" TEXT NULL,
                        "ReviewCardId" TEXT NOT NULL,
                        "Rating" INTEGER NOT NULL,
                        "ResponseTimeMs" INTEGER NOT NULL,
                        "ResultingStatus" INTEGER NOT NULL,
                        "ResultingIntervalDays" INTEGER NOT NULL,
                        "CreatedAt" TEXT NOT NULL,
                        "UpdatedAt" TEXT NOT NULL,
                        CONSTRAINT "FK_StudyLogs_ReviewCards_ReviewCardId" FOREIGN KEY ("ReviewCardId") REFERENCES "ReviewCards" ("Id") ON DELETE CASCADE,
                        CONSTRAINT "FK_StudyLogs_StudySessions_StudySessionId" FOREIGN KEY ("StudySessionId") REFERENCES "StudySessions" ("Id") ON DELETE SET NULL
                    );
                    INSERT INTO "StudyLogs_temp" SELECT "Id", "StudySessionId", "ReviewCardId", "Rating", "ResponseTimeMs", "ResultingStatus", "ResultingIntervalDays", "CreatedAt", "UpdatedAt" FROM "StudyLogs";
                    DROP TABLE "StudyLogs";
                    ALTER TABLE "StudyLogs_temp" RENAME TO "StudyLogs";
                    CREATE INDEX IF NOT EXISTS "IX_StudyLogs_CreatedAt" ON "StudyLogs" ("CreatedAt");
                    CREATE INDEX IF NOT EXISTS "IX_StudyLogs_ReviewCardId" ON "StudyLogs" ("ReviewCardId");
                    CREATE INDEX IF NOT EXISTS "IX_StudyLogs_StudySessionId" ON "StudyLogs" ("StudySessionId");
                    PRAGMA foreign_keys = ON;
                    """;
                await _context.Database.ExecuteSqlRawAsync(migrationSql, cancellationToken);
                _logger.LogInformation("StudyLogs table successfully migrated.");
            }

            const string createStorageTableSql = """
                CREATE TABLE IF NOT EXISTS "LearningStorageItems" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_LearningStorageItems" PRIMARY KEY,
                    "ItemType" INTEGER NOT NULL,
                    "WordId" TEXT NULL,
                    "GrammarRuleId" TEXT NULL,
                    "Status" INTEGER NOT NULL,
                    "ReasonAdded" INTEGER NOT NULL,
                    "Note" TEXT NULL,
                    "IncorrectCount" INTEGER NOT NULL DEFAULT 0,
                    "CorrectCount" INTEGER NOT NULL DEFAULT 0,
                    "EaseFactor" REAL NOT NULL DEFAULT 2.5,
                    "IntervalDays" INTEGER NOT NULL DEFAULT 1,
                    "Repetitions" INTEGER NOT NULL DEFAULT 0,
                    "Lapses" INTEGER NOT NULL DEFAULT 0,
                    "RetentionProbability" REAL NOT NULL DEFAULT 50.0,
                    "LastReviewedAt" TEXT NULL,
                    "NextReviewAt" TEXT NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_LearningStorageItems_Words_WordId" FOREIGN KEY ("WordId") REFERENCES "Words" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_LearningStorageItems_GrammarRules_GrammarRuleId" FOREIGN KEY ("GrammarRuleId") REFERENCES "GrammarRules" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_LearningStorageItems_ItemType" ON "LearningStorageItems" ("ItemType");
                CREATE INDEX IF NOT EXISTS "IX_LearningStorageItems_Status" ON "LearningStorageItems" ("Status");
                CREATE INDEX IF NOT EXISTS "IX_LearningStorageItems_NextReviewAt" ON "LearningStorageItems" ("NextReviewAt");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_LearningStorageItems_WordId" ON "LearningStorageItems" ("WordId") WHERE "WordId" IS NOT NULL;
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_LearningStorageItems_GrammarRuleId" ON "LearningStorageItems" ("GrammarRuleId") WHERE "GrammarRuleId" IS NOT NULL;
                CREATE INDEX IF NOT EXISTS "IX_ReviewCards_Status_NextReviewAt" ON "ReviewCards" ("Status", "NextReviewAt");
                CREATE INDEX IF NOT EXISTS "IX_StudyLogs_ReviewCardId_CreatedAt" ON "StudyLogs" ("ReviewCardId", "CreatedAt");
                CREATE INDEX IF NOT EXISTS "IX_StudyLogs_StudySessionId_CreatedAt" ON "StudyLogs" ("StudySessionId", "CreatedAt");
                """;
            await _context.Database.ExecuteSqlRawAsync(createStorageTableSql, cancellationToken);

            const string createHomophonesTablesSql = """
                CREATE TABLE IF NOT EXISTS "HomophoneGroups" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_HomophoneGroups" PRIMARY KEY,
                    "Title" TEXT NOT NULL,
                    "PhoneticIpa" TEXT NOT NULL DEFAULT '',
                    "Type" INTEGER NOT NULL DEFAULT 0,
                    "DifficultyLevel" INTEGER NOT NULL DEFAULT 0,
                    "ExamTag" INTEGER NOT NULL DEFAULT 0,
                    "DifferentiationTip" TEXT NULL,
                    "Notes" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS "IX_HomophoneGroups_Title" ON "HomophoneGroups" ("Title");
                CREATE INDEX IF NOT EXISTS "IX_HomophoneGroups_Type" ON "HomophoneGroups" ("Type");
                CREATE INDEX IF NOT EXISTS "IX_HomophoneGroups_DifficultyLevel" ON "HomophoneGroups" ("DifficultyLevel");
                CREATE INDEX IF NOT EXISTS "IX_HomophoneGroups_ExamTag" ON "HomophoneGroups" ("ExamTag");
                CREATE INDEX IF NOT EXISTS "IX_HomophoneGroups_CreatedAt" ON "HomophoneGroups" ("CreatedAt");

                CREATE TABLE IF NOT EXISTS "HomophoneItems" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_HomophoneItems" PRIMARY KEY,
                    "HomophoneGroupId" TEXT NOT NULL,
                    "Term" TEXT NOT NULL,
                    "NormalizedTerm" TEXT NOT NULL,
                    "PartOfSpeech" INTEGER NOT NULL DEFAULT 0,
                    "MeaningVi" TEXT NOT NULL,
                    "MeaningEn" TEXT NULL,
                    "PhoneticSpecific" TEXT NULL,
                    "ExampleSentence" TEXT NULL,
                    "ExampleTranslation" TEXT NULL,
                    "AudioPath" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_HomophoneItems_HomophoneGroups_HomophoneGroupId" FOREIGN KEY ("HomophoneGroupId") REFERENCES "HomophoneGroups" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_HomophoneItems_HomophoneGroupId" ON "HomophoneItems" ("HomophoneGroupId");
                CREATE INDEX IF NOT EXISTS "IX_HomophoneItems_Term" ON "HomophoneItems" ("Term");
                CREATE INDEX IF NOT EXISTS "IX_HomophoneItems_NormalizedTerm" ON "HomophoneItems" ("NormalizedTerm");
                """;
            await _context.Database.ExecuteSqlRawAsync(createHomophonesTablesSql, cancellationToken);

            const string createRoadmapsTablesSql = """
                CREATE TABLE IF NOT EXISTS "StudyRoadmaps" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_StudyRoadmaps" PRIMARY KEY,
                    "Title" TEXT NOT NULL,
                    "Target" INTEGER NOT NULL,
                    "StartingScore" REAL NOT NULL,
                    "TargetScore" REAL NOT NULL,
                    "TargetDate" TEXT NOT NULL,
                    "IsActive" INTEGER NOT NULL DEFAULT 1,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS "IX_StudyRoadmaps_Target" ON "StudyRoadmaps" ("Target");
                CREATE INDEX IF NOT EXISTS "IX_StudyRoadmaps_IsActive" ON "StudyRoadmaps" ("IsActive");
                CREATE INDEX IF NOT EXISTS "IX_StudyRoadmaps_CreatedAt" ON "StudyRoadmaps" ("CreatedAt");

                CREATE TABLE IF NOT EXISTS "RoadmapMilestones" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_RoadmapMilestones" PRIMARY KEY,
                    "RoadmapId" TEXT NOT NULL,
                    "Title" TEXT NOT NULL,
                    "Description" TEXT NULL,
                    "OrderIndex" INTEGER NOT NULL,
                    "TargetCompletionDate" TEXT NULL,
                    "IsCompleted" INTEGER NOT NULL DEFAULT 0,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_RoadmapMilestones_StudyRoadmaps_RoadmapId" FOREIGN KEY ("RoadmapId") REFERENCES "StudyRoadmaps" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_RoadmapMilestones_RoadmapId" ON "RoadmapMilestones" ("RoadmapId");
                CREATE INDEX IF NOT EXISTS "IX_RoadmapMilestones_OrderIndex" ON "RoadmapMilestones" ("OrderIndex");

                CREATE TABLE IF NOT EXISTS "RoadmapNodes" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_RoadmapNodes" PRIMARY KEY,
                    "MilestoneId" TEXT NOT NULL,
                    "Title" TEXT NOT NULL,
                    "Description" TEXT NULL,
                    "Type" INTEGER NOT NULL,
                    "Status" INTEGER NOT NULL,
                    "OrderIndex" INTEGER NOT NULL,
                    "CanvasX" REAL NOT NULL,
                    "CanvasY" REAL NOT NULL,
                    "TargetItemCount" INTEGER NOT NULL,
                    "CompletedItemCount" INTEGER NOT NULL,
                    "EaseFactorWeight" REAL NOT NULL DEFAULT 2.5,
                    "ReferenceId" TEXT NULL,
                    "ReferenceName" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_RoadmapNodes_RoadmapMilestones_MilestoneId" FOREIGN KEY ("MilestoneId") REFERENCES "RoadmapMilestones" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_RoadmapNodes_MilestoneId" ON "RoadmapNodes" ("MilestoneId");
                CREATE INDEX IF NOT EXISTS "IX_RoadmapNodes_Status" ON "RoadmapNodes" ("Status");
                CREATE INDEX IF NOT EXISTS "IX_RoadmapNodes_Type" ON "RoadmapNodes" ("Type");
                CREATE INDEX IF NOT EXISTS "IX_RoadmapNodes_OrderIndex" ON "RoadmapNodes" ("OrderIndex");
                """;
            await _context.Database.ExecuteSqlRawAsync(createRoadmapsTablesSql, cancellationToken);

            const string createPhrasesTableSql = """
                CREATE TABLE IF NOT EXISTS "ConversationPhrases" (
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
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_Category" ON "ConversationPhrases" ("Category");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_CefrLevel" ON "ConversationPhrases" ("CefrLevel");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_IsFavorite" ON "ConversationPhrases" ("IsFavorite");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_CreatedAt" ON "ConversationPhrases" ("CreatedAt");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_NormalizedPrompt" ON "ConversationPhrases" ("NormalizedPrompt");
                """;
            await _context.Database.ExecuteSqlRawAsync(createPhrasesTableSql, cancellationToken);

            // Ensure ReferenceName column exists on RoadmapNodes for existing databases
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = "PRAGMA table_info(\"RoadmapNodes\");";
                using var checkReader = await checkCmd.ExecuteReaderAsync(cancellationToken);
                bool hasReferenceName = false;
                while (await checkReader.ReadAsync(cancellationToken))
                {
                    if (string.Equals(checkReader.GetString(1), "ReferenceName", StringComparison.OrdinalIgnoreCase))
                    {
                        hasReferenceName = true;
                        break;
                    }
                }
                checkReader.Close();

                if (!hasReferenceName)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE \"RoadmapNodes\" ADD COLUMN \"ReferenceName\" TEXT NULL;",
                        cancellationToken);
                }
            }

            // Ensure FSRS Stability and Difficulty columns exist on ReviewCards for existing databases
            using (var checkRcCmd = connection.CreateCommand())
            {
                checkRcCmd.CommandText = "PRAGMA table_info(\"ReviewCards\");";
                using var rcReader = await checkRcCmd.ExecuteReaderAsync(cancellationToken);
                bool hasStability = false;
                bool hasDifficulty = false;
                while (await rcReader.ReadAsync(cancellationToken))
                {
                    var colName = rcReader.GetString(1);
                    if (string.Equals(colName, "Stability", StringComparison.OrdinalIgnoreCase))
                        hasStability = true;
                    else if (string.Equals(colName, "Difficulty", StringComparison.OrdinalIgnoreCase))
                        hasDifficulty = true;
                }
                rcReader.Close();

                if (!hasStability)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE \"ReviewCards\" ADD COLUMN \"Stability\" REAL NOT NULL DEFAULT 0.0;",
                        cancellationToken);
                }

                if (!hasDifficulty)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE \"ReviewCards\" ADD COLUMN \"Difficulty\" REAL NOT NULL DEFAULT 5.0;",
                        cancellationToken);
                }
            }

            // Deduplicate duplicate ExamTags and Topics if any exist in the database
            try
            {
                var allTags = await _context.ExamTags.ToListAsync(cancellationToken);
                var dupTagGroups = allTags
                    .GroupBy(t => t.Name.Trim().ToLowerInvariant())
                    .Where(g => g.Count() > 1)
                    .ToList();

                if (dupTagGroups.Count > 0)
                {
                    _logger.LogInformation("Deduplicating {Count} duplicate ExamTag groups in database...", dupTagGroups.Count);
                    foreach (var group in dupTagGroups)
                    {
                        var primary = group.First();
                        var duplicates = group.Skip(1).ToList();
                        var dupIds = duplicates.Select(d => d.Id).ToList();

                        var wordExamTags = await _context.WordExamTags
                            .Where(wet => dupIds.Contains(wet.ExamTagId))
                            .ToListAsync(cancellationToken);

                        foreach (var wet in wordExamTags)
                        {
                            bool alreadyLinked = await _context.WordExamTags
                                .AnyAsync(w => w.WordId == wet.WordId && w.ExamTagId == primary.Id, cancellationToken);

                            if (alreadyLinked)
                            {
                                _context.WordExamTags.Remove(wet);
                            }
                            else
                            {
                                wet.ExamTagId = primary.Id;
                            }
                        }

                        _context.ExamTags.RemoveRange(duplicates);
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                }

                var allTopicsList = await _context.Topics.ToListAsync(cancellationToken);
                var dupTopicGroups = allTopicsList
                    .GroupBy(t => t.Name.Trim().ToLowerInvariant())
                    .Where(g => g.Count() > 1)
                    .ToList();

                if (dupTopicGroups.Count > 0)
                {
                    _logger.LogInformation("Deduplicating {Count} duplicate Topic groups in database...", dupTopicGroups.Count);
                    foreach (var group in dupTopicGroups)
                    {
                        var primary = group.First();
                        var duplicates = group.Skip(1).ToList();
                        var dupIds = duplicates.Select(d => d.Id).ToList();

                        var wordTopics = await _context.WordTopics
                            .Where(wt => dupIds.Contains(wt.TopicId))
                            .ToListAsync(cancellationToken);

                        foreach (var wt in wordTopics)
                        {
                            bool alreadyLinked = await _context.WordTopics
                                .AnyAsync(w => w.WordId == wt.WordId && w.TopicId == primary.Id, cancellationToken);

                            if (alreadyLinked)
                            {
                                _context.WordTopics.Remove(wt);
                            }
                            else
                            {
                                wt.TopicId = primary.Id;
                            }
                        }

                        _context.Topics.RemoveRange(duplicates);
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tag/Topic deduplication encountered an issue.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Schema compatibility check encountered an issue (can be ignored if table does not exist yet).");
        }
    }

    private async Task SeedHomophoneStarterDataAsync(CancellationToken ct)
    {
        var groups = new List<HomophoneGroup>
        {
            new()
            {
                Title = "bare / bear",
                PhoneticIpa = "/beə(r)/",
                Type = HomophoneType.ExactHomophone,
                DifficultyLevel = CefrLevel.B1,
                ExamTag = ExamType.Toeic,
                DifferentiationTip = "'Bare' là tính từ nghĩa là trần trụi, trống không (vd: bare feet). 'Bear' là động từ chịu đựng hoặc danh từ con gấu.",
                Items =
                {
                    new HomophoneItem
                    {
                        Term = "bare",
                        NormalizedTerm = "bare",
                        PartOfSpeech = PartOfSpeech.Adjective,
                        MeaningVi = "trần trụi, trống không, trọc",
                        MeaningEn = "not clothed or covered",
                        ExampleSentence = "He walked on the cold wooden floor with bare feet.",
                        ExampleTranslation = "Anh ấy đi chân trần trên sàn gỗ lạnh lẽo."
                    },
                    new HomophoneItem
                    {
                        Term = "bear",
                        NormalizedTerm = "bear",
                        PartOfSpeech = PartOfSpeech.Verb,
                        MeaningVi = "chịu đựng (hoặc danh từ: con gấu)",
                        MeaningEn = "to endure, to carry or tolerate",
                        ExampleSentence = "I can hardly bear the continuous construction noise.",
                        ExampleTranslation = "Tôi khó lòng mà chịu đựng nổi tiếng ồn xây dựng liên tục này."
                    }
                }
            },
            new()
            {
                Title = "knight / night",
                PhoneticIpa = "/naɪt/",
                Type = HomophoneType.ExactHomophone,
                DifficultyLevel = CefrLevel.B1,
                ExamTag = ExamType.Ielts,
                DifferentiationTip = "'Knight' bắt đầu bằng 'k' câm nghĩa là hiệp sĩ thời trung cổ. 'Night' nghĩa là ban đêm.",
                Items =
                {
                    new HomophoneItem
                    {
                        Term = "knight",
                        NormalizedTerm = "knight",
                        PartOfSpeech = PartOfSpeech.Noun,
                        MeaningVi = "hiệp sĩ, quân mã trong cờ vua",
                        MeaningEn = "a medieval warrior of noble birth",
                        ExampleSentence = "The brave knight pledged allegiance to the royal kingdom.",
                        ExampleTranslation = "Người hiệp sĩ dũng cảm đã tuyên thệ trung thành với vương quốc hoàng gia."
                    },
                    new HomophoneItem
                    {
                        Term = "night",
                        NormalizedTerm = "night",
                        PartOfSpeech = PartOfSpeech.Noun,
                        MeaningVi = "ban đêm, đêm tối",
                        MeaningEn = "the period of darkness between sunset and sunrise",
                        ExampleSentence = "The sky is filled with glowing stars tonight.",
                        ExampleTranslation = "Bầu trời đêm nay tràn ngập những vì sao tỏa sáng."
                    }
                }
            },
            new()
            {
                Title = "flour / flower",
                PhoneticIpa = "/flaʊ.ər/",
                Type = HomophoneType.ExactHomophone,
                DifficultyLevel = CefrLevel.A2,
                ExamTag = ExamType.General,
                DifferentiationTip = "'Flour' là bột mì làm bánh. 'Flower' là bông hoa tươi.",
                Items =
                {
                    new HomophoneItem
                    {
                        Term = "flour",
                        NormalizedTerm = "flour",
                        PartOfSpeech = PartOfSpeech.Noun,
                        MeaningVi = "bột mì (nguyên liệu nấu nướng/làm bánh)",
                        MeaningEn = "powder made by grinding grain, used in baking",
                        ExampleSentence = "Sift the flour carefully before mixing it with butter.",
                        ExampleTranslation = "Hãy rây bột mì cẩn thận trước khi trộn cùng với bơ."
                    },
                    new HomophoneItem
                    {
                        Term = "flower",
                        NormalizedTerm = "flower",
                        PartOfSpeech = PartOfSpeech.Noun,
                        MeaningVi = "bông hoa",
                        MeaningEn = "the seed-bearing part of a plant, consisting of reproductive organs",
                        ExampleSentence = "The garden is full of colourful spring flowers.",
                        ExampleTranslation = "Khu vườn ngập tràn những bông hoa mùa xuân đầy màu sắc."
                    }
                }
            },
            new()
            {
                Title = "peace / piece",
                PhoneticIpa = "/piːs/",
                Type = HomophoneType.ExactHomophone,
                DifficultyLevel = CefrLevel.A2,
                ExamTag = ExamType.General,
                DifferentiationTip = "'Peace' là hòa bình, bình yên. 'Piece' là mẩu, mảnh, tác phẩm (vd: a piece of cake).",
                Items =
                {
                    new HomophoneItem
                    {
                        Term = "peace",
                        NormalizedTerm = "peace",
                        PartOfSpeech = PartOfSpeech.Noun,
                        MeaningVi = "sự hòa bình, yên bình",
                        MeaningEn = "freedom from disturbance; tranquility",
                        ExampleSentence = "The ambassadors met to discuss lasting peace in the region.",
                        ExampleTranslation = "Các vị đại sứ đã họp mặt để thảo luận về nền hòa bình lâu dài trong khu vực."
                    },
                    new HomophoneItem
                    {
                        Term = "piece",
                        NormalizedTerm = "piece",
                        PartOfSpeech = PartOfSpeech.Noun,
                        MeaningVi = "mảnh, mẩu, khúc, tác phẩm",
                        MeaningEn = "a portion of an object or of material",
                        ExampleSentence = "Would you like another piece of chocolate cake?",
                        ExampleTranslation = "Bạn có muốn ăn thêm một miếng bánh sô-cô-la nữa không?"
                    }
                }
            },
            new()
            {
                Title = "weather / whether",
                PhoneticIpa = "/ˈweð.ər/",
                Type = HomophoneType.ExactHomophone,
                DifficultyLevel = CefrLevel.B1,
                ExamTag = ExamType.Toeic,
                DifferentiationTip = "'Weather' là thời tiết (nắng, mưa). 'Whether' là từ nối liên từ nghĩa là 'liệu rằng' (whether... or).",
                Items =
                {
                    new HomophoneItem
                    {
                        Term = "weather",
                        NormalizedTerm = "weather",
                        PartOfSpeech = PartOfSpeech.Noun,
                        MeaningVi = "thời tiết, khí hậu",
                        MeaningEn = "the state of the atmosphere at a place and time",
                        ExampleSentence = "We decided to postpone the picnic due to severe weather.",
                        ExampleTranslation = "Chúng tôi quyết định hoãn buổi dã ngoại vì thời tiết khắc nghiệt."
                    },
                    new HomophoneItem
                    {
                        Term = "whether",
                        NormalizedTerm = "whether",
                        PartOfSpeech = PartOfSpeech.Conjunction,
                        MeaningVi = "liệu rằng, có hay không",
                        MeaningEn = "expressing a doubt or choice between alternatives",
                        ExampleSentence = "He hasn't decided whether to accept the new job offer.",
                        ExampleTranslation = "Anh ấy vẫn chưa quyết định liệu có nên nhận lời mời làm việc mới hay không."
                    }
                }
            },
            new()
            {
                Title = "desert / dessert",
                PhoneticIpa = "/ˈdez.ət/ vs /dɪˈzɜːt/",
                Type = HomophoneType.NearHomophone,
                DifficultyLevel = CefrLevel.B1,
                ExamTag = ExamType.Toeic,
                DifferentiationTip = "Bẫy trọng âm: 'Desert' trọng âm 1 nghĩa là sa mạc (hoặc đt từ bỏ). 'Dessert' (2 chữ s - double sweet) trọng âm 2 nghĩa là món tráng miệng ngọt ngào.",
                Items =
                {
                    new HomophoneItem
                    {
                        Term = "desert",
                        NormalizedTerm = "desert",
                        PartOfSpeech = PartOfSpeech.Noun,
                        MeaningVi = "sa mạc (hoặc động từ: đào ngũ, rời bỏ)",
                        MeaningEn = "a dry, barren area of land",
                        PhoneticSpecific = "/ˈdez.ət/",
                        ExampleSentence = "The Sahara is the largest hot desert in the world.",
                        ExampleTranslation = "Sahara là sa mạc cát nóng lớn nhất trên thế giới."
                    },
                    new HomophoneItem
                    {
                        Term = "dessert",
                        NormalizedTerm = "dessert",
                        PartOfSpeech = PartOfSpeech.Noun,
                        MeaningVi = "món tráng miệng (sau bữa ăn chính)",
                        MeaningEn = "the sweet course eaten at the end of a meal",
                        PhoneticSpecific = "/dɪˈzɜːt/",
                        ExampleSentence = "We had fresh fruit and ice cream for dessert.",
                        ExampleTranslation = "Chúng tôi đã thưởng thức trái cây tươi và kem cho món tráng miệng."
                    }
                }
            },
            new()
            {
                Title = "there / their / they're",
                PhoneticIpa = "/ðeə(r)/",
                Type = HomophoneType.ExactHomophone,
                DifficultyLevel = CefrLevel.A2,
                ExamTag = ExamType.Toeic,
                DifferentiationTip = "Bộ 3 đồng âm kinh điển: 'There' chỉ nơi chốn (ở đó) hoặc sự tồn tại (there is/are); 'Their' là tính từ sở hữu (của họ); 'They're' là viết tắt của 'they are' (họ là/đang).",
                Items =
                {
                    new HomophoneItem
                    {
                        Term = "there",
                        NormalizedTerm = "there",
                        PartOfSpeech = PartOfSpeech.Adverb,
                        MeaningVi = "ở đó, tại nơi đó",
                        MeaningEn = "in, at, or to that place or position",
                        ExampleSentence = "Please put the conference documents over there.",
                        ExampleTranslation = "Vui lòng đặt các tài liệu hội nghị ở đằng kia."
                    },
                    new HomophoneItem
                    {
                        Term = "their",
                        NormalizedTerm = "their",
                        PartOfSpeech = PartOfSpeech.Adjective,
                        MeaningVi = "của họ, của chúng (tính từ sở hữu)",
                        MeaningEn = "belonging to or associated with the people previously mentioned",
                        ExampleSentence = "The engineers presented their innovative prototype to the board.",
                        ExampleTranslation = "Các kỹ sư đã trình bày nguyên mẫu sáng tạo của họ trước hội đồng."
                    },
                    new HomophoneItem
                    {
                        Term = "they're",
                        NormalizedTerm = "they're",
                        PartOfSpeech = PartOfSpeech.Phrase,
                        MeaningVi = "họ đang / họ là (viết tắt của they are)",
                        MeaningEn = "contraction of they are",
                        ExampleSentence = "They're preparing for the upcoming IELTS examination.",
                        ExampleTranslation = "Họ đang chuẩn bị cho kỳ thi IELTS sắp tới."
                    }
                }
            }
        };

        await _context.HomophoneGroups.AddRangeAsync(groups, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Successfully seeded {Count} starter homophone groups.", groups.Count);
    }

    private async Task SeedPhraseStarterDataAsync(CancellationToken ct)
    {
        var phrases = new List<ConversationPhrase>
        {
            // Category 1: Greetings & Introductions
            new()
            {
                Category = "Greetings & Introductions",
                ContextTag = "Daily Greeting",
                PromptSentence = "Good morning! How are you doing today?",
                PromptTranslation = "Chào buổi sáng! Hôm nay bạn thế nào?",
                SampleResponse = "Good morning! I'm doing really well, thank you. How about yourself?",
                ResponseTranslation = "Chào buổi sáng! Tôi rất khỏe, cảm ơn bạn. Còn bạn thì sao?",
                AdditionalSuggestions = "• Formal: \"Good morning, Mr./Ms. [Name]. I trust you had a restful weekend?\"\n• Casual: \"Morning! How's everything going?\"\n• Pro Tip: Luôn đối đáp lại bằng \"How about yourself?\" hoặc \"And you?\" để giữ phép lịch sự tự nhiên.",
                CefrLevel = CefrLevel.A2,
                IsFavorite = true
            },
            new()
            {
                Category = "Greetings & Introductions",
                ContextTag = "First Meeting",
                PromptSentence = "It's a great pleasure to finally meet you in person.",
                PromptTranslation = "Rất hân hạnh khi cuối cùng cũng được gặp trực tiếp bạn.",
                SampleResponse = "Likewise! I've been really looking forward to our collaboration.",
                ResponseTranslation = "Tôi cũng vậy! Tôi đã rất mong chờ sự hợp tác giữa chúng ta.",
                AdditionalSuggestions = "• Business Formal: \"The pleasure is all mine. Thank you for taking the time to meet today.\"\n• Vocabulary: \"Likewise\" là từ đối đáp lịch thiệp thay cho \"Me too\".",
                CefrLevel = CefrLevel.B1,
                IsFavorite = true
            },
            new()
            {
                Category = "Greetings & Introductions",
                ContextTag = "New Member",
                PromptSentence = "I'd like to introduce myself. My name is Alex, and I just joined the product team.",
                PromptTranslation = "Tôi xin phép được giới thiệu bản thân. Tên tôi là Alex và tôi vừa gia nhập đội ngũ phát triển sản phẩm.",
                SampleResponse = "Welcome aboard, Alex! It's fantastic to have you on the team. Let me know if you need any help settling in.",
                ResponseTranslation = "Chào mừng bạn gia nhập công ty, Alex! Rất tuyệt khi có bạn trong đội ngũ. Hãy cho tôi biết nếu bạn cần giúp đỡ gì để hòa nhập nhé.",
                AdditionalSuggestions = "• Idioms: \"Welcome aboard\" (chào mừng gia nhập), \"settle in\" (làm quen, ổn định môi trường mới).\n• Casual: \"Hey Alex, great to meet you! Excited to collaborate!\"",
                CefrLevel = CefrLevel.B1
            },

            // Category 2: Workplace & Office
            new()
            {
                Category = "Workplace & Office",
                ContextTag = "Project Status",
                PromptSentence = "Could you please give me a quick status update on the quarterly report?",
                PromptTranslation = "Bạn có thể cập nhật nhanh cho tôi tình hình báo cáo quý được không?",
                SampleResponse = "Certainly! We have finalized the financial summary and are currently reviewing the executive metrics. It will be ready by 3 PM.",
                ResponseTranslation = "Chắc chắn rồi! Chúng tôi đã chốt xong bản tóm tắt tài chính và hiện đang rà soát các chỉ số điều hành. Báo cáo sẽ sẵn sàng trước 3 giờ chiều.",
                AdditionalSuggestions = "• Collocations: \"ahead of schedule\" (vượt tiến độ), \"on track\" (đúng tiến độ), \"finalized the draft\" (chốt bản thảo).\n• Follow-up: \"Would you like a preliminary walkthrough before the final submission?\"",
                CefrLevel = CefrLevel.B2,
                IsFavorite = true
            },
            new()
            {
                Category = "Workplace & Office",
                ContextTag = "Scheduling",
                PromptSentence = "Would you mind if we rescheduled our sync meeting to tomorrow morning?",
                PromptTranslation = "Bạn có phiền nếu chúng ta dời cuộc họp trao đổi sang sáng mai không?",
                SampleResponse = "Not at all! Tomorrow morning works perfectly for me. Does 9:30 AM suit your calendar?",
                ResponseTranslation = "Không sao cả! Sáng mai hoàn toàn phù hợp với tôi. Lúc 9:30 sáng có tiện lịch của bạn không?",
                AdditionalSuggestions = "• Grammar Note: Khi được hỏi \"Would you mind...?\", câu trả lời \"Not at all\" hoặc \"No problem\" biểu thị sự đồng ý (không thấy phiền).",
                CefrLevel = CefrLevel.B2
            },
            new()
            {
                Category = "Workplace & Office",
                ContextTag = "Proposal",
                PromptSentence = "I'd like to propose a minor adjustment to our delivery timeline.",
                PromptTranslation = "Tôi muốn đề xuất một điều chỉnh nhỏ đối với mốc thời gian bàn giao của chúng ta.",
                SampleResponse = "I understand. Could you please share the rationale behind the adjustment and how it mitigates potential risks?",
                ResponseTranslation = "Tôi hiểu rồi. Bạn có thể chia sẻ lý do đằng sau đề xuất điều chỉnh này và cách nó giảm thiểu các rủi ro tiềm ẩn không?",
                AdditionalSuggestions = "• Advanced vocab: \"mitigate risks\" (giảm thiểu rủi ro), \"rationale behind\" (lý do căn bản), \"delivery timeline\" (tiến độ bàn giao).",
                CefrLevel = CefrLevel.C1
            },

            // Category 3: Job Interviews
            new()
            {
                Category = "Job Interviews",
                ContextTag = "Candidate Strengths",
                PromptSentence = "Why do you believe you are the strongest candidate for this position?",
                PromptTranslation = "Tại sao bạn tin rằng mình là ứng viên sáng giá nhất cho vị trí này?",
                SampleResponse = "With over five years of dedicated experience in full-stack architecture and a solid track record of optimizing system throughput, I can immediately streamline your team's development lifecycle.",
                ResponseTranslation = "Với hơn 5 năm kinh nghiệm chuyên sâu về kiến trúc full-stack và thành tích vững chắc trong việc tối ưu hóa hiệu năng hệ thống, tôi có thể ngay lập tức tinh gọn vòng đời phát triển của nhóm bạn.",
                AdditionalSuggestions = "• Interview Structure: Nền tảng chuyên môn + Minh chứng kết quả (track record) + Đóng góp tức thời (streamline lifecycle).\n• Alternative: \"My proactive mindset aligns seamlessly with your engineering standards.\"",
                CefrLevel = CefrLevel.B2,
                IsFavorite = true
            },
            new()
            {
                Category = "Job Interviews",
                ContextTag = "Pressure Management",
                PromptSentence = "How do you handle tight deadlines and high-pressure situations?",
                PromptTranslation = "Bạn xử lý các hạn chót gấp gáp và tình huống áp lực cao như thế nào?",
                SampleResponse = "I prioritize tasks based on impact and urgency, break complex challenges into manageable milestones, and keep stakeholders transparently informed.",
                ResponseTranslation = "Tôi sắp xếp công việc theo mức độ ảnh hưởng và tính cấp thiết, chia nhỏ thách thức phức tạp thành các cột mốc dễ quản lý và luôn thông báo minh bạch cho các bên liên quan.",
                AdditionalSuggestions = "• STAR Method: Chuẩn bị một ví dụ ngắn có kết quả định lượng cụ thể khi người phỏng vấn hỏi đào sâu (drill-down).",
                CefrLevel = CefrLevel.B2
            },

            // Category 4: Travel & Directions
            new()
            {
                Category = "Travel & Directions",
                ContextTag = "Asking Directions",
                PromptSentence = "Excuse me, could you point me toward the nearest underground station?",
                PromptTranslation = "Xin lỗi, bạn có thể chỉ giúp tôi đường đến ga tàu điện ngầm gần nhất không?",
                SampleResponse = "Sure thing! Walk straight down this avenue for two blocks, then take the first right. You can't miss the entrance next to the pharmacy.",
                ResponseTranslation = "Được chứ! Đi thẳng đại lộ này qua hai dãy nhà, rồi rẽ phải ở ngã rẽ đầu tiên. Lối vào ngay cạnh hiệu thuốc rất dễ thấy.",
                AdditionalSuggestions = "• Native Phrasing: \"You can't miss it\" (rất dễ nhận thấy).\n• Directions: \"within walking distance\" (ở cự ly đi bộ được), \"hang a left/right\" (rẽ trái/phải).",
                CefrLevel = CefrLevel.A2
            },
            new()
            {
                Category = "Travel & Directions",
                ContextTag = "Hotel Check-in",
                PromptSentence = "Hello, I have a hotel reservation under the name Tran for three nights.",
                PromptTranslation = "Xin chào, tôi có đặt phòng khách sạn trước dưới tên Trần trong ba đêm.",
                SampleResponse = "Welcome, Mr. Tran! I have found your booking right here. May I please see your passport and a credit card for the incidental deposit?",
                ResponseTranslation = "Chào mừng quý khách Trần! Tôi đã tìm thấy thông tin đặt phòng của quý khách ở đây. Tôi có thể xin phép xem hộ chiếu và thẻ tín dụng để đặt cọc chi phí phát sinh không ạ?",
                AdditionalSuggestions = "• Travel Vocab: \"incidental deposit\" (tiền cọc chi phí phát sinh), \"complimentary breakfast\" (bữa sáng miễn phí đi kèm).",
                CefrLevel = CefrLevel.B1
            },

            // Category 5: Dining & Shopping
            new()
            {
                Category = "Dining & Shopping",
                ContextTag = "Paying the Bill",
                PromptSentence = "Could we please get the check? And do you split bills among guests?",
                PromptTranslation = "Làm ơn cho chúng tôi xin hóa đơn được không? Quán có hỗ trợ chia tiền hóa đơn không ạ?",
                SampleResponse = "Certainly! Here is your bill. Yes, we can easily split the total evenly or charge individual items as you prefer.",
                ResponseTranslation = "Chắc chắn rồi! Đây là hóa đơn của quý khách. Vâng, chúng tôi có thể dễ dàng chia đều tổng số tiền hoặc thanh toán riêng từng món tùy ý quý khách.",
                AdditionalSuggestions = "• Usage Note: Người Mỹ dùng \"the check\", người Anh dùng \"the bill\". \"Go Dutch\" hoặc \"split the bill\" nghĩa là chia tiền.",
                CefrLevel = CefrLevel.A2
            },
            new()
            {
                Category = "Dining & Shopping",
                ContextTag = "Recommendations",
                PromptSentence = "What do you recommend for someone who loves seafood but prefers less spicy food?",
                PromptTranslation = "Bạn gợi ý món gì cho người thích hải sản nhưng thích ăn ít cay?",
                SampleResponse = "I would highly recommend our pan-seared sea bass with white wine sauce. It is exceptionally fresh, flavorful, and completely mild.",
                ResponseTranslation = "Tôi rất khuyến khích quý khách thử món cá chẽm áp chảo với sốt vang trắng. Món này cực kỳ tươi ngon, đậm vị và hoàn toàn không cay.",
                AdditionalSuggestions = "• Culinary Terms: \"pan-seared\" (áp chảo), \"exceptionally fresh\" (cực kỳ tươi ngon), \"mild\" (thanh dịu, không cay).",
                CefrLevel = CefrLevel.B1
            },

            // Category 6: TOEIC & VSTEP Speaking
            new()
            {
                Category = "TOEIC & VSTEP Speaking",
                ContextTag = "Opinion Expression",
                PromptSentence = "In your opinion, what is the primary benefit of continuous online learning?",
                PromptTranslation = "Theo quan điểm của bạn, lợi ích hàng đầu của việc học tập trực tuyến liên tục là gì?",
                SampleResponse = "From my perspective, the primary benefit is autonomy. Learners can tailor their pace according to personal needs, which dramatically optimizes knowledge retention while balancing professional obligations.",
                ResponseTranslation = "Từ góc nhìn của tôi, lợi ích hàng đầu là tính tự chủ. Người học có thể điều chỉnh tốc độ theo nhu cầu cá nhân, điều này tối ưu hóa đáng kể khả năng ghi nhớ kiến thức trong khi vẫn cân bằng được các nghĩa vụ công việc.",
                AdditionalSuggestions = "• Academic Connectors: \"From my perspective\", \"Consequently\", \"Furthermore\".\n• Collocations: \"tailor their pace\", \"knowledge retention\", \"balance professional obligations\".",
                CefrLevel = CefrLevel.B2,
                IsFavorite = true
            },
            new()
            {
                Category = "TOEIC & VSTEP Speaking",
                ContextTag = "Debate & Comparison",
                PromptSentence = "Do you think artificial intelligence will replace human educators in the near future?",
                PromptTranslation = "Bạn có nghĩ trí tuệ nhân tạo sẽ thay thế các nhà giáo dục trong tương lai gần không?",
                SampleResponse = "While AI can personalize curriculum and automate grading efficiently, it cannot replicate empathy, moral mentorship, and emotional encouragement that human educators provide.",
                ResponseTranslation = "Mặc dù AI có thể cá nhân hóa chương trình học và tự động chấm điểm hiệu quả, nó không thể tái tạo được sự thấu cảm, sự dìu dắt đạo đức và sự động viên cảm xúc mà các nhà giáo dục mang lại.",
                AdditionalSuggestions = "• Balanced View: \"While [advantage of AI], it cannot replicate [human essence]\".\n• C1 Vocab: \"replicate empathy\", \"moral mentorship\".",
                CefrLevel = CefrLevel.C1
            },

            // Category 7: Daily Conversation & Socializing
            new()
            {
                Category = "Daily Conversation & Socializing",
                ContextTag = "Catching Up",
                PromptSentence = "It's been ages since we last caught up! How have you been holding up?",
                PromptTranslation = "Đã lâu lắm rồi chúng ta mới gặp lại nhau! Dạo này bạn thế nào rồi?",
                SampleResponse = "I know, right? Time flies! I've been quite busy with my new project, but overall everything is going great. How about you?",
                ResponseTranslation = "Chuẩn luôn! Thời gian trôi nhanh thật! Dạo này mình khá bận rộn với dự án mới, nhưng nhìn chung mọi thứ đều tuyệt. Còn cậu thì sao?",
                AdditionalSuggestions = "• Idioms: \"It's been ages\" (lâu lắm rồi), \"Time flies\" (thời gian trôi nhanh), \"catch up\" (hàn huyên tâm sự).\n• Casual response: \"Can't complain! Hanging in there.\"",
                CefrLevel = CefrLevel.B1,
                IsFavorite = true
            }
        };

        foreach (var phrase in phrases)
        {
            phrase.UpdateNormalizedFields();
        }

        await _context.ConversationPhrases.AddRangeAsync(phrases, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Successfully seeded {Count} starter conversation phrases.", phrases.Count);
    }
}
