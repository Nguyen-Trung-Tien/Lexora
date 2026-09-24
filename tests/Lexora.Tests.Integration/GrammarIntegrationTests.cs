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

public class GrammarIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LexoraDbContext _context;
    private readonly GrammarRepository _grammarRepo;
    private readonly UnitOfWork _unitOfWork;
    private readonly GrammarService _grammarService;

    public GrammarIntegrationTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LexoraDbContext(options);

        var initializer = new SqliteDatabaseInitializer(_context, NullLogger<SqliteDatabaseInitializer>.Instance);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _grammarRepo = new GrammarRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
        _grammarService = new GrammarService(_grammarRepo, _unitOfWork);
    }

    [Fact]
    public async Task GrammarCrud_Integration_ShouldCreateUpdateFilterAndDelete()
    {
        // 1. Initial seeded rules
        var initialRules = await _grammarService.GetAllRulesAsync();
        initialRules.Should().HaveCount(2);

        // 2. Create new rule with examples
        var createDto = new GrammarRuleCreateOrUpdateDto(
            Id: null,
            Title: "Inversion with Negative Adverbials",
            Structure: "Negative Adverbial + Auxiliary + Subject + Main Verb",
            ExplanationVi: "Đảo ngữ với các trạng từ phủ định nhằm nhấn mạnh.",
            ExplanationEn: "Inversion after negative or restrictive expressions.",
            CommonMistakes: "Forgetting to invert subject and auxiliary verb.",
            Level: CefrLevel.C1,
            ExamTarget: ExamType.Vstep,
            Topic: "Inversion",
            Examples:
            [
                new("Never have I seen such a beautiful sunset.", "Chưa bao giờ tôi nhìn thấy cảnh hoàng hôn đẹp như vậy.", "Emphasis on 'Never'")
            ]
        );

        var created = await _grammarService.CreateRuleAsync(createDto);
        created.Should().NotBeNull();
        created.Title.Should().Be("Inversion with Negative Adverbials");

        // Verify in DB
        var dbRule = await _context.GrammarRules.Include(gr => gr.Examples).FirstOrDefaultAsync(gr => gr.Id == created.Id);
        dbRule.Should().NotBeNull();
        dbRule!.Level.Should().Be(CefrLevel.C1);
        dbRule.ExamTarget.Should().Be(ExamType.Vstep);
        dbRule.Examples.Should().HaveCount(1);

        // 3. Filter by CEFR C1
        var c1Rules = await _grammarService.GetFilteredRulesAsync(level: CefrLevel.C1);
        c1Rules.Should().ContainSingle(r => r.Id == created.Id);

        // 4. Update the rule
        var updateDto = new GrammarRuleCreateOrUpdateDto(
            Id: created.Id,
            Title: "Inversion with Negative Adverbials (Updated)",
            Structure: created.Structure,
            ExplanationVi: created.ExplanationVi,
            ExplanationEn: created.ExplanationEn,
            CommonMistakes: created.CommonMistakes,
            Level: CefrLevel.C1,
            ExamTarget: ExamType.Ielts,
            Topic: "Advanced Grammar",
            Examples:
            [
                new("Rarely do we witness such dedication.", "Hiếm khi chúng ta được chứng kiến sự cống hiến như vậy.", null)
            ]
        );

        var updated = await _grammarService.UpdateRuleAsync(updateDto);
        updated.Title.Should().Be("Inversion with Negative Adverbials (Updated)");
        updated.ExamTarget.Should().Be(ExamType.Ielts);
        updated.Examples.Should().HaveCount(1);
        updated.Examples[0].SentenceEn.Should().Be("Rarely do we witness such dedication.");

        // 5. Generate Grammar Quiz from SQLite
        var quizQuestions = await _grammarService.GenerateGrammarQuizAsync(count: 2);
        quizQuestions.Should().HaveCount(2);
        quizQuestions.All(q => q.Options.Count == 4).Should().BeTrue();

        // 6. Delete the rule
        await _grammarService.DeleteRuleAsync(created.Id);
        var deleted = await _context.GrammarRules.FindAsync(created.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task GrammarUpdate_ConsecutiveUpdates_ShouldRetainAndSyncExamples()
    {
        // 1. Create rule with 2 examples
        var rule = await _grammarService.CreateRuleAsync(new GrammarRuleCreateOrUpdateDto(
            Id: null,
            Title: "First Conditional",
            Structure: "If + present simple, ... will + infinitive",
            ExplanationVi: "Câu điều kiện loại 1",
            ExplanationEn: "Possible real condition in the future",
            CommonMistakes: "Using 'will' in the if-clause",
            Level: CefrLevel.A2,
            ExamTarget: ExamType.Toeic,
            Topic: "Conditionals",
            Examples:
            [
                new("If it rains, we will stay at home.", "Nếu trời mưa, chúng tôi sẽ ở nhà.", null),
                new("If you study hard, you will pass.", "Nếu bạn học chăm, bạn sẽ đỗ.", null)
            ]
        ));

        rule.Examples.Should().HaveCount(2);

        // 2. First update: change to 3 examples
        var update1 = await _grammarService.UpdateRuleAsync(new GrammarRuleCreateOrUpdateDto(
            Id: rule.Id,
            Title: "First Conditional (Updated 1)",
            Structure: rule.Structure,
            ExplanationVi: rule.ExplanationVi,
            ExplanationEn: rule.ExplanationEn,
            CommonMistakes: rule.CommonMistakes,
            Level: CefrLevel.B1,
            ExamTarget: ExamType.Toeic,
            Topic: "Conditionals",
            Examples:
            [
                new("If she calls, tell her I am busy.", "Nếu cô ấy gọi, hãy bảo tôi bận.", null),
                new("If you eat too much, you will feel sick.", "Nếu bạn ăn quá nhiều, bạn sẽ thấy khó chịu.", null),
                new("If they arrive late, we will start without them.", "Nếu họ đến muộn, chúng ta sẽ bắt đầu trước.", null)
            ]
        ));

        update1.Title.Should().Be("First Conditional (Updated 1)");
        update1.Examples.Should().HaveCount(3);

        // Verify direct from repository query
        var dbFetch1 = await _grammarService.GetRuleByIdAsync(rule.Id);
        dbFetch1.Should().NotBeNull();
        dbFetch1!.Examples.Should().HaveCount(3);

        // 3. Second consecutive update: change to 1 example
        var update2 = await _grammarService.UpdateRuleAsync(new GrammarRuleCreateOrUpdateDto(
            Id: rule.Id,
            Title: "First Conditional (Updated 2)",
            Structure: rule.Structure,
            ExplanationVi: rule.ExplanationVi,
            ExplanationEn: rule.ExplanationEn,
            CommonMistakes: rule.CommonMistakes,
            Level: CefrLevel.B2,
            ExamTarget: ExamType.Ielts,
            Topic: "Conditionals",
            Examples:
            [
                new("If we hurry, we can still catch the train.", "Nếu chúng ta nhanh lên, chúng ta vẫn kịp tàu.", "Modal variation")
            ]
        ));

        update2.Title.Should().Be("First Conditional (Updated 2)");
        update2.Examples.Should().HaveCount(1);
        update2.Examples[0].Note.Should().Be("Modal variation");

        var dbFetch2 = await _grammarService.GetRuleByIdAsync(rule.Id);
        dbFetch2.Should().NotBeNull();
        dbFetch2!.Examples.Should().HaveCount(1);
        dbFetch2.Examples[0].SentenceEn.Should().Be("If we hurry, we can still catch the train.");
    }

    [Fact]
    public async Task DeleteRulesAsync_MultipleRules_ShouldDeleteAllAndCascadeExamples()
    {
        // 1. Create two grammar rules with examples
        var rule1 = await _grammarService.CreateRuleAsync(new GrammarRuleCreateOrUpdateDto(
            Id: null,
            Title: "Rule 1 Bulk",
            Structure: "S + V1",
            ExplanationVi: "Giải thích 1",
            ExplanationEn: "Explanation 1",
            CommonMistakes: null,
            Level: CefrLevel.A1,
            ExamTarget: ExamType.Toeic,
            Topic: "Basics",
            Examples: [new("Example 1", "Ví dụ 1", null)]
        ));

        var rule2 = await _grammarService.CreateRuleAsync(new GrammarRuleCreateOrUpdateDto(
            Id: null,
            Title: "Rule 2 Bulk",
            Structure: "S + V2",
            ExplanationVi: "Giải thích 2",
            ExplanationEn: "Explanation 2",
            CommonMistakes: null,
            Level: CefrLevel.A2,
            ExamTarget: ExamType.Toeic,
            Topic: "Basics",
            Examples: [new("Example 2", "Ví dụ 2", null)]
        ));

        // 2. Bulk delete
        var deletedCount = await _grammarService.DeleteRulesAsync(new[] { rule1.Id, rule2.Id });
        deletedCount.Should().Be(2);

        // 3. Verify rules and examples are deleted
        var r1 = await _context.GrammarRules.FindAsync(rule1.Id);
        var r2 = await _context.GrammarRules.FindAsync(rule2.Id);
        r1.Should().BeNull();
        r2.Should().BeNull();

        var examples = await _context.GrammarExamples.Where(ge => ge.GrammarRuleId == rule1.Id || ge.GrammarRuleId == rule2.Id).ToListAsync();
        examples.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
