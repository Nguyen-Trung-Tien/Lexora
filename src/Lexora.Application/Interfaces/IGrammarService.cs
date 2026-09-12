using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Application.Interfaces;

public interface IGrammarService
{
    Task<IReadOnlyList<GrammarRuleDto>> GetAllRulesAsync(CancellationToken cancellationToken = default);
    Task<GrammarRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrammarRuleDto>> GetRulesByLevelAsync(CefrLevel level, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrammarRuleDto>> SearchRulesAsync(string query, CancellationToken cancellationToken = default);
}
