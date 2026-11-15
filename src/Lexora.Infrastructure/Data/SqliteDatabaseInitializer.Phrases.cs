using System.Data;
using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Data;

public partial class SqliteDatabaseInitializer
{
    private static readonly (string ColumnName, string Definition)[] ConversationPhraseMigrationColumns =
    [
        ("Register", "INTEGER NOT NULL DEFAULT 0"),
        ("ExamTarget", "INTEGER NULL"),
        ("KeyPhrases", "TEXT NOT NULL DEFAULT ''"),
        ("FormalVariation", "TEXT NOT NULL DEFAULT ''"),
        ("CasualVariation", "TEXT NOT NULL DEFAULT ''"),
        ("CommonMistakesVi", "TEXT NOT NULL DEFAULT ''"),
        ("Status", "INTEGER NOT NULL DEFAULT 0"),
        ("EaseFactor", "REAL NOT NULL DEFAULT 2.5"),
        ("IntervalDays", "INTEGER NOT NULL DEFAULT 0"),
        ("Repetitions", "INTEGER NOT NULL DEFAULT 0"),
        ("Lapses", "INTEGER NOT NULL DEFAULT 0"),
        ("LastReviewedAt", "TEXT NULL"),
        ("NextReviewAt", "TEXT NULL"),
        ("LinkedWordId", "TEXT NULL")
    ];

    private async Task EnsureConversationPhraseColumnsAndIndexesAsync(CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        bool shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = """PRAGMA table_info("ConversationPhrases");""";
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (!reader.IsDBNull(1))
                    {
                        existingColumns.Add(reader.GetString(1));
                    }
                }
            }

            foreach (var (columnName, definition) in ConversationPhraseMigrationColumns)
            {
                if (!existingColumns.Contains(columnName))
                {
                    var alterSql = $"ALTER TABLE \"ConversationPhrases\" ADD COLUMN \"{columnName}\" {definition};";
                    await _context.Database.ExecuteSqlRawAsync(alterSql, cancellationToken);
                }
            }

            const string indexSql = """
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_Category" ON "ConversationPhrases" ("Category");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_ContextTag" ON "ConversationPhrases" ("ContextTag");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_Register" ON "ConversationPhrases" ("Register");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_ExamTarget" ON "ConversationPhrases" ("ExamTarget");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_CefrLevel" ON "ConversationPhrases" ("CefrLevel");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_IsFavorite" ON "ConversationPhrases" ("IsFavorite");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_Status" ON "ConversationPhrases" ("Status");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_NextReviewAt" ON "ConversationPhrases" ("NextReviewAt");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_CreatedAt" ON "ConversationPhrases" ("CreatedAt");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_NormalizedPrompt" ON "ConversationPhrases" ("NormalizedPrompt");
                CREATE INDEX IF NOT EXISTS "IX_ConversationPhrases_Category_CefrLevel_Register" ON "ConversationPhrases" ("Category", "CefrLevel", "Register");
                """;
            await _context.Database.ExecuteSqlRawAsync(indexSql, cancellationToken);
        }
        finally
        {
            if (shouldClose && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task SeedEnrichedPhraseStarterDataAsync(CancellationToken cancellationToken)
    {
        bool hadPhrasesBefore = await _context.ConversationPhrases.AnyAsync(cancellationToken);
        await SeedPhraseStarterDataAsync(cancellationToken);

        if (hadPhrasesBefore)
        {
            return;
        }

        var existing = await _context.ConversationPhrases.ToListAsync(cancellationToken);
        var byPrompt = existing
            .GroupBy(p => p.NormalizedPrompt, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var phrase in existing)
        {
            EnrichExistingStarterMetadata(phrase);
        }

        var scenarioExtras = BuildAdditionalScenarioStarterPhrases();
        foreach (var extra in scenarioExtras)
        {
            extra.UpdateNormalizedFields();
            if (!byPrompt.ContainsKey(extra.NormalizedPrompt))
            {
                await _context.ConversationPhrases.AddAsync(extra, cancellationToken);
                byPrompt[extra.NormalizedPrompt] = extra;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void EnrichExistingStarterMetadata(ConversationPhrase phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase.KeyPhrases) && !string.IsNullOrWhiteSpace(phrase.SampleResponse))
        {
            var words = phrase.SampleResponse.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
            phrase.KeyPhrases = words.Length > 0 ? words[0].Trim() : phrase.ContextTag;
        }

        if (phrase.Category.Contains("Workplace", StringComparison.OrdinalIgnoreCase) ||
            phrase.Category.Contains("Business", StringComparison.OrdinalIgnoreCase))
        {
            phrase.Register = PhraseRegister.Formal;
            phrase.ExamTarget ??= ExamTarget.Toeic;
        }
        else if (phrase.Category.Contains("Academic", StringComparison.OrdinalIgnoreCase) ||
                 phrase.Category.Contains("IELTS", StringComparison.OrdinalIgnoreCase))
        {
            phrase.Register = PhraseRegister.Formal;
            phrase.ExamTarget ??= ExamTarget.Ielts;
        }
        else if (phrase.Category.Contains("VSTEP", StringComparison.OrdinalIgnoreCase))
        {
            phrase.Register = PhraseRegister.Neutral;
            phrase.ExamTarget ??= ExamTarget.Vstep;
        }

        if (string.IsNullOrWhiteSpace(phrase.CommonMistakesVi))
        {
            phrase.CommonMistakesVi = "Lưu ý ngữ điệu tự nhiên và tránh dịch sát từng từ từ tiếng Việt.";
        }
    }

    private static List<ConversationPhrase> BuildAdditionalScenarioStarterPhrases() =>
    [
        new()
        {
            Category = "Business & TOEIC",
            ContextTag = "Budget Approval",
            CefrLevel = CefrLevel.B2,
            Register = PhraseRegister.Formal,
            ExamTarget = ExamTarget.Toeic,
            PromptSentence = "Could you clarify the cost breakdown for the Q4 marketing campaign?",
            PromptTranslation = "Bạn có thể làm rõ bảng phân bổ chi phí cho chiến dịch marketing quý 4 không?",
            SampleResponse = "Certainly. The main allocation covers digital ads, while the remaining 20% is set aside for contingency.",
            ResponseTranslation = "Chắc chắn rồi. Khoản phân bổ chính dành cho quảng cáo số, phần 20% còn lại được dự phòng cho các tình huống phát sinh.",
            KeyPhrases = "cost breakdown; main allocation; set aside for contingency",
            FormalVariation = "I would be glad to walk you through the itemized Q4 budget allocation.",
            CasualVariation = "Sure — most of it goes to digital ads, and we kept 20% aside as a buffer.",
            CommonMistakesVi = "Tránh viết 'clarify about the cost' (clarify đi trực tiếp với tân ngữ, không dùng 'about').",
            AdditionalSuggestions = "Follow up by asking: Would you like me to share the updated spreadsheet after the meeting?"
        },
        new()
        {
            Category = "Academic & IELTS Speaking",
            ContextTag = "Part 3 Opinion",
            CefrLevel = CefrLevel.C1,
            Register = PhraseRegister.Formal,
            ExamTarget = ExamTarget.Ielts,
            PromptSentence = "Do you think remote work will permanently replace traditional office spaces?",
            PromptTranslation = "Bạn có nghĩ làm việc từ xa sẽ thay thế vĩnh viễn văn phòng truyền thống không?",
            SampleResponse = "To a certain extent, yes. However, a hybrid model strikes a healthier balance between flexibility and team cohesion.",
            ResponseTranslation = "Ở một mức độ nhất định thì có. Tuy nhiên, mô hình kết hợp tạo ra sự cân bằng lành mạnh hơn giữa tính linh hoạt và sự gắn kết đội nhóm.",
            KeyPhrases = "To a certain extent; strikes a balance; team cohesion",
            FormalVariation = "While telecommuting offers undeniable autonomy, hybrid arrangements are likely to prevail.",
            CasualVariation = "Kind of, yeah — though I think a hybrid setup works best for keeping teams connected.",
            CommonMistakesVi = "Không nói 'In my opinion, I think...' (trùng lặp ý); chọn một trong hai cụm.",
            AdditionalSuggestions = "Give a concrete contrast example (collaborative brainstorming vs deep solo work)."
        },
        new()
        {
            Category = "Travel & Airport",
            ContextTag = "Hotel Check-in",
            CefrLevel = CefrLevel.B1,
            Register = PhraseRegister.Neutral,
            ExamTarget = ExamTarget.Toeic,
            PromptSentence = "We arrived a bit early—would it be possible to leave our luggage here before check-in?",
            PromptTranslation = "Chúng tôi đến hơi sớm — liệu có thể gửi hành lý ở đây trước giờ nhận phòng không?",
            SampleResponse = "Of course! We'd be happy to store your bags at the concierge desk until your room is ready at 2 PM.",
            ResponseTranslation = "Tất nhiên rồi ạ! Chúng tôi rất sẵn lòng giữ hành lý tại quầy lễ tân cho đến khi phòng sẵn sàng lúc 2 giờ chiều.",
            KeyPhrases = "arrived a bit early; store your bags; concierge desk",
            FormalVariation = "Certainly, our bell desk would be pleased to hold your luggage until check-in opens.",
            CasualVariation = "No problem at all! Just drop your bags here and we'll look after them until two.",
            CommonMistakesVi = "Lưu ý 'luggage' là danh từ không đếm được — không thêm 's' thành 'luggages'."
        },
        new()
        {
            Category = "VSTEP Practical Situations",
            ContextTag = "Part 2 Solution Discussion",
            CefrLevel = CefrLevel.B2,
            Register = PhraseRegister.Neutral,
            ExamTarget = ExamTarget.Vstep,
            PromptSentence = "Among public buses, cycling, and carpooling, which option best suits daily commuters in a crowded city?",
            PromptTranslation = "Giữa xe buýt công cộng, đi xe đạp và đi chung xe, phương án nào phù hợp nhất với người đi làm hằng ngày ở thành phố đông đúc?",
            SampleResponse = "I would opt for public buses primarily because they are cost-effective, safe in bad weather, and significantly ease traffic congestion.",
            ResponseTranslation = "Tôi sẽ chọn xe buýt công cộng chủ yếu vì nó tiết kiệm chi phí, an toàn khi thời tiết xấu và giảm đáng kể ùn tắc giao thông.",
            KeyPhrases = "opt for; cost-effective; ease traffic congestion",
            FormalVariation = "Public transit stands out as the most sustainable and economical alternative for urban commuters.",
            CasualVariation = "Honestly, taking the bus makes the most sense—it's cheap and avoids the traffic hassle.",
            CommonMistakesVi = "Trong VSTEP Part 2, cần nêu rõ lý do chọn 1 phương án VÀ giải thích ngắn gọn vì sao loại 2 phương án còn lại."
        },
        new()
        {
            Category = "Daily Life & Social",
            ContextTag = "Polite Declining",
            CefrLevel = CefrLevel.B1,
            Register = PhraseRegister.Idiomatic,
            PromptSentence = "Are you free to grab dinner after work tonight?",
            PromptTranslation = "Tối nay tan làm bạn có rảnh đi ăn tối không?",
            SampleResponse = "I'd love to, but I have a lot on my plate tonight. Can I take a rain check for Friday?",
            ResponseTranslation = "Mình rất muốn đi, nhưng tối nay mình nhiều việc quá. Để hẹn bạn sang thứ Sáu được không?",
            KeyPhrases = "a lot on my plate; take a rain check",
            FormalVariation = "Thank you for the invitation, though I have prior commitments tonight. Could we reschedule for Friday?",
            CasualVariation = "Sounds awesome, but tonight's packed for me—how about Friday instead?",
            CommonMistakesVi = "Không nên từ chối thẳng 'No, I am busy tonight'; hãy cảm ơn + nêu lý do ngắn + đề xuất thời gian khác."
        }
    ];
}
