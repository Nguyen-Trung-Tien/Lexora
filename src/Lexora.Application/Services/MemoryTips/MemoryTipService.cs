using Lexora.Application.DTOs.MemoryTips;
using Lexora.Application.Interfaces.MemoryTips;
using Lexora.Domain.Entities.MemoryTips;
using Lexora.Domain.Enums;
using Lexora.Domain.Exceptions;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services.MemoryTips;

/// <summary>
/// Business orchestration service implementing CRUD, validation, pinning, and pagination for MemoryTips.
/// Enforces single-commit atomic transactions via IUnitOfWork and throws DomainValidationException for invalid payloads.
/// </summary>
public class MemoryTipService : IMemoryTipService
{
    private readonly IMemoryTipRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MemoryTipService(
        IMemoryTipRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<MemoryTipDto>> GetTipsAsync(
        TipCategory? category = null,
        MnemonicTechnique? technique = null,
        ExamTarget? examTarget = null,
        bool? onlyPinned = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetFilteredAsync(
            category,
            technique,
            examTarget,
            onlyPinned,
            search,
            cancellationToken);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<MemoryTipsPagedResultDto> GetPagedTipsAsync(
        int page,
        int pageSize,
        TipCategory? category = null,
        MnemonicTechnique? technique = null,
        ExamTarget? examTarget = null,
        bool? onlyPinned = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var validPage = Math.Max(1, page);
        var validPageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _repository.GetPagedAsync(
            validPage,
            validPageSize,
            category,
            technique,
            examTarget,
            onlyPinned,
            search,
            cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new MemoryTipsPagedResultDto(dtos, totalCount, validPage, validPageSize);
    }

    public async Task<MemoryTipDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<MemoryTipDto> CreateAsync(CreateMemoryTipRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request.Title, request.MnemonicHook, request.Explanation, request.Example, request.CommonTrap);

        var entity = new MemoryTip
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            MnemonicHook = request.MnemonicHook.Trim(),
            Explanation = request.Explanation.Trim(),
            Example = string.IsNullOrWhiteSpace(request.Example) ? null : request.Example.Trim(),
            CommonTrap = string.IsNullOrWhiteSpace(request.CommonTrap) ? null : request.CommonTrap.Trim(),
            Category = request.Category,
            Technique = request.Technique,
            ExamTarget = request.ExamTarget,
            IsPinned = request.IsPinned,
            Tags = string.IsNullOrWhiteSpace(request.Tags) ? null : request.Tags.Trim(),
            LinkedWordId = request.LinkedWordId,
            LinkedGrammarRuleId = request.LinkedGrammarRuleId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<MemoryTipDto> UpdateAsync(UpdateMemoryTipRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request.Title, request.MnemonicHook, request.Explanation, request.Example, request.CommonTrap);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(MemoryTip), request.Id);

        entity.Title = request.Title.Trim();
        entity.MnemonicHook = request.MnemonicHook.Trim();
        entity.Explanation = request.Explanation.Trim();
        entity.Example = string.IsNullOrWhiteSpace(request.Example) ? null : request.Example.Trim();
        entity.CommonTrap = string.IsNullOrWhiteSpace(request.CommonTrap) ? null : request.CommonTrap.Trim();
        entity.Category = request.Category;
        entity.Technique = request.Technique;
        entity.ExamTarget = request.ExamTarget;
        entity.IsPinned = request.IsPinned;
        entity.Tags = string.IsNullOrWhiteSpace(request.Tags) ? null : request.Tags.Trim();
        entity.LinkedWordId = request.LinkedWordId;
        entity.LinkedGrammarRuleId = request.LinkedGrammarRuleId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MemoryTipDto> TogglePinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(MemoryTip), id);

        entity.IsPinned = !entity.IsPinned;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<IReadOnlyList<MemoryTipDto>> GetPinnedTipsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetPinnedTipsAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<MemoryTipDto>> GetByWordIdAsync(Guid wordId, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetByLinkedWordIdAsync(wordId, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<MemoryTipDto>> GetByGrammarRuleIdAsync(Guid grammarRuleId, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetByLinkedGrammarRuleIdAsync(grammarRuleId, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<MemoryTipStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var all = await _repository.GetAllAsync(cancellationToken);
        return new MemoryTipStatisticsDto
        {
            TotalCount = all.Count,
            PinnedCount = all.Count(t => t.IsPinned),
            TrapCount = all.Count(t => !string.IsNullOrWhiteSpace(t.CommonTrap)),
            VocabularyCount = all.Count(t => t.Category == TipCategory.Vocabulary),
            GrammarCount = all.Count(t => t.Category == TipCategory.Grammar),
            ExamStrategyCount = all.Count(t => t.Category == TipCategory.ExamStrategy)
        };
    }

    public async Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetTagsAsync(cancellationToken);
    }

    private static void ValidateRequest(string title, string mnemonicHook, string explanation, string? example, string? commonTrap)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException(nameof(title), "Tiêu đề mẹo ghi nhớ không được để trống.");
        }

        if (title.Trim().Length > 250)
        {
            throw new DomainValidationException(nameof(title), "Tiêu đề không được vượt quá 250 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(mnemonicHook))
        {
            throw new DomainValidationException(nameof(mnemonicHook), "Câu thần chú / mẹo nhớ nhanh không được để trống.");
        }

        if (mnemonicHook.Trim().Length > 1000)
        {
            throw new DomainValidationException(nameof(mnemonicHook), "Câu thần chú không được vượt quá 1000 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(explanation))
        {
            throw new DomainValidationException(nameof(explanation), "Nội dung giải thích mẹo không được để trống.");
        }

        if (explanation.Trim().Length > 3000)
        {
            throw new DomainValidationException(nameof(explanation), "Nội dung giải thích không được vượt quá 3000 ký tự.");
        }

        if (example != null && example.Trim().Length > 2000)
        {
            throw new DomainValidationException(nameof(example), "Ví dụ minh họa không được vượt quá 2000 ký tự.");
        }

        if (commonTrap != null && commonTrap.Trim().Length > 2000)
        {
            throw new DomainValidationException(nameof(commonTrap), "Bẫy đề thi không được vượt quá 2000 ký tự.");
        }
    }

    private static MemoryTipDto MapToDto(MemoryTip entity)
    {
        return new MemoryTipDto
        {
            Id = entity.Id,
            Title = entity.Title,
            MnemonicHook = entity.MnemonicHook,
            Explanation = entity.Explanation,
            Example = entity.Example,
            CommonTrap = entity.CommonTrap,
            Category = entity.Category,
            Technique = entity.Technique,
            ExamTarget = entity.ExamTarget,
            IsPinned = entity.IsPinned,
            Tags = entity.Tags,
            LinkedWordId = entity.LinkedWordId,
            LinkedGrammarRuleId = entity.LinkedGrammarRuleId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
