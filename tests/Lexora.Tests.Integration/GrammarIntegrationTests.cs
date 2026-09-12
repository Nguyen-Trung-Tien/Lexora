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

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
