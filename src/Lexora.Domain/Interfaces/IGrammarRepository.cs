using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Interfaces;

public interface IGrammarRepository : IRepository<GrammarRule>
{
    Task<GrammarRule?> GetWithExamplesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrammarRule>> GetByLevelAsync(CefrLevel level, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrammarRule>> GetByExamTargetAsync(ExamType examType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrammarRule>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
