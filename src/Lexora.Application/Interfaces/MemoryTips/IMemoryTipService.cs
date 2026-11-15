using Lexora.Application.DTOs.MemoryTips;
using Lexora.Domain.Enums;

namespace Lexora.Application.Interfaces.MemoryTips;

/// <summary>
/// Business orchestration contract for Memory Tips and Mnemonic Hooks.
/// </summary>
public interface IMemoryTipService
{
    Task<IReadOnlyList<MemoryTipDto>> GetTipsAsync(
        TipCategory? category = null,
        MnemonicTechnique? technique = null,
        ExamTarget? examTarget = null,
        bool? onlyPinned = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<MemoryTipsPagedResultDto> GetPagedTipsAsync(
        int page,
        int pageSize,
        TipCategory? category = null,
        MnemonicTechnique? technique = null,
        ExamTarget? examTarget = null,
        bool? onlyPinned = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<MemoryTipDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MemoryTipDto> CreateAsync(CreateMemoryTipRequest request, CancellationToken cancellationToken = default);

    Task<MemoryTipDto> UpdateAsync(UpdateMemoryTipRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MemoryTipDto> TogglePinAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryTipDto>> GetPinnedTipsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryTipDto>> GetByWordIdAsync(Guid wordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryTipDto>> GetByGrammarRuleIdAsync(Guid grammarRuleId, CancellationToken cancellationToken = default);

    Task<MemoryTipStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default);
}
