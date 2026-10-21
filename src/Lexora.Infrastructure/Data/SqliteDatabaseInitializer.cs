using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
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
        try
        {
            _logger.LogInformation("Ensuring SQLite database is created...");
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the SQLite database.");
            throw;
        }
    }

    private async Task SeedInitialDataAsync(CancellationToken ct)
    {
        // 1. Topics
        var businessTopic = new Topic
        {
            Name = "Business & Career",
            Description = "Vocabularies for office, meetings, and business communication",
            IconGlyph = "\uE825",
            ColorHex = "#2B579A"
        };
        var techTopic = new Topic
        {
            Name = "Technology",
            Description = "Software, computers, digital innovation",
            IconGlyph = "\uE770",
            ColorHex = "#0078D4"
        };
        var dailyTopic = new Topic
        {
            Name = "Daily Life",
            Description = "Everyday conversation, routines, hobbies",
            IconGlyph = "\uE80F",
            ColorHex = "#107C41"
        };

        await _context.Topics.AddRangeAsync(new[] { businessTopic, techTopic, dailyTopic }, ct);

        // 2. Exam Tags
        var toeicTag = new ExamTag { Name = "TOEIC 650+", Type = ExamType.Toeic, TargetScore = "650" };
        var vstepTag = new ExamTag { Name = "VSTEP B2", Type = ExamType.Vstep, TargetScore = "B2" };
        var ieltsTag = new ExamTag { Name = "IELTS 6.5", Type = ExamType.Ielts, TargetScore = "6.5" };

        await _context.ExamTags.AddRangeAsync(new[] { toeicTag, vstepTag, ieltsTag }, ct);

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
            }
        };

        await _context.HomophoneGroups.AddRangeAsync(groups, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Successfully seeded {Count} starter homophone groups.", groups.Count);
    }
}
