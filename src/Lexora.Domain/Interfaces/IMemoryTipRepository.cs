using Lexora.Domain.Entities.MemoryTips;
using Lexora.Domain.Enums;

namespace Lexora.Domain.Interfaces;

/// <summary>
/// Repository contract for querying and persisting memory tips, mnemonic hooks, and exam strategies.
/// </summary>
public interface IMemoryTipRepository : IRepository<MemoryTip>
{
    Task<IReadOnlyList<MemoryTip>> GetFilteredAsync(
        TipCategory? category = null,
        MnemonicTechnique? technique = null,
        ExamTarget? examTarget = null,
        bool? onlyPinned = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<MemoryTip> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        TipCategory? category = null,
        MnemonicTechnique? technique = null,
        ExamTarget? examTarget = null,
        bool? onlyPinned = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryTip>> GetPinnedTipsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryTip>> GetByLinkedWordIdAsync(Guid wordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryTip>> GetByLinkedGrammarRuleIdAsync(Guid grammarRuleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default);
}
