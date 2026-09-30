using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Application.Interfaces;

public interface IGrammarService
{
    Task<IReadOnlyList<GrammarRuleDto>> GetAllRulesAsync(CancellationToken cancellationToken = default);
    Task<GrammarRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrammarRuleDto>> GetRulesByLevelAsync(CefrLevel level, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrammarRuleDto>> SearchRulesAsync(string query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrammarRuleDto>> GetFilteredRulesAsync(CefrLevel? level = null, ExamType? examType = null, string? topic = null, string? search = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAllTopicsAsync(CancellationToken cancellationToken = default);
    Task<GrammarRuleDto> CreateRuleAsync(GrammarRuleCreateOrUpdateDto dto, CancellationToken cancellationToken = default);
    Task<GrammarRuleDto> UpdateRuleAsync(GrammarRuleCreateOrUpdateDto dto, CancellationToken cancellationToken = default);
    Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> DeleteRulesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrammarQuizQuestionDto>> GenerateGrammarQuizAsync(int count = 10, CefrLevel? level = null, ExamType? examType = null, CancellationToken cancellationToken = default);
    GrammarPracticeResultDto EvaluateQuiz(List<GrammarQuizQuestionDto> questions, List<GrammarQuizSubmissionDto> answers);
    Task<GrammarPagedResultDto> GetPagedRulesAsync(int page, int pageSize, CefrLevel? level = null, ExamType? examType = null, string? topic = null, string? search = null, CancellationToken cancellationToken = default);
}
