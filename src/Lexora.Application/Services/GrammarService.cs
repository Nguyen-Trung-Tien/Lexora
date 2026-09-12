using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class GrammarService : IGrammarService
{
    private readonly IGrammarRepository _grammarRepo;

    public GrammarService(IGrammarRepository grammarRepo)
    {
        _grammarRepo = grammarRepo;
    }

    public async Task<IReadOnlyList<GrammarRuleDto>> GetAllRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _grammarRepo.GetAllAsync(cancellationToken);
        return rules.Select(MapToDto).ToList();
    }

    public async Task<GrammarRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _grammarRepo.GetWithExamplesAsync(id, cancellationToken);
        return rule == null ? null : MapToDto(rule);
    }

    public async Task<IReadOnlyList<GrammarRuleDto>> GetRulesByLevelAsync(CefrLevel level, CancellationToken cancellationToken = default)
    {
        var rules = await _grammarRepo.GetByLevelAsync(level, cancellationToken);
        return rules.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<GrammarRuleDto>> SearchRulesAsync(string query, CancellationToken cancellationToken = default)
    {
        var rules = await _grammarRepo.SearchAsync(query, cancellationToken);
        return rules.Select(MapToDto).ToList();
    }

    private static GrammarRuleDto MapToDto(GrammarRule r)
    {
        var examples = r.Examples.Select(e => new GrammarExampleDto(e.Id, e.SentenceEn, e.SentenceVi, e.Note)).ToList();
        return new GrammarRuleDto(
            r.Id,
            r.Title,
            r.Structure,
            r.ExplanationVi,
            r.ExplanationEn,
            r.CommonMistakes,
            r.Level,
            r.ExamTarget,
            r.Topic,
            examples
        );
    }
}
