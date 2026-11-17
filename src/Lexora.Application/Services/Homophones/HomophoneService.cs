using Lexora.Application.Common;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class HomophoneService : IHomophoneService
{
    private readonly IHomophoneRepository _homophoneRepo;
    private readonly IUnitOfWork _unitOfWork;

    public HomophoneService(IHomophoneRepository homophoneRepo, IUnitOfWork unitOfWork)
    {
        _homophoneRepo = homophoneRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<HomophoneGroupDto>> GetAllGroupsAsync(
        HomophoneType? typeFilter = null,
        CefrLevel? levelFilter = null,
        ExamType? examFilter = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            var groups = await _homophoneRepo.GetFilteredAsync(
                type: typeFilter,
                level: levelFilter,
                examType: examFilter,
                query: null,
                cancellationToken: cancellationToken);

            return groups.Select(MapToDto).ToList();
        }

        var candidates = await _homophoneRepo.GetFilteredAsync(
            type: typeFilter,
            level: levelFilter,
            examType: examFilter,
            query: null,
            cancellationToken: cancellationToken);

        return RankHomophones(candidates, searchTerm);
    }

    public async Task<HomophoneGroupDto?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _homophoneRepo.GetWithItemsAsync(id, cancellationToken);
        return group == null ? null : MapToDto(group);
    }

    public async Task<HomophoneGroupDto> CreateGroupAsync(CreateHomophoneGroupDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Group title cannot be empty.", nameof(dto));
        }

        var group = new HomophoneGroup
        {
            Title = dto.Title.Trim(),
            PhoneticIpa = dto.PhoneticIpa?.Trim() ?? string.Empty,
            Type = dto.Type,
            DifficultyLevel = dto.DifficultyLevel,
            ExamTag = dto.ExamTag,
            DifferentiationTip = dto.DifferentiationTip?.Trim(),
            Notes = dto.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var itemDto in dto.Items)
        {
            if (string.IsNullOrWhiteSpace(itemDto.Term)) continue;

            group.Items.Add(new HomophoneItem
            {
                HomophoneGroupId = group.Id,
                Term = itemDto.Term.Trim(),
                NormalizedTerm = itemDto.Term.Trim().ToLowerInvariant(),
                PartOfSpeech = itemDto.PartOfSpeech,
                MeaningVi = itemDto.MeaningVi?.Trim() ?? string.Empty,
                MeaningEn = itemDto.MeaningEn?.Trim(),
                PhoneticSpecific = itemDto.PhoneticSpecific?.Trim(),
                ExampleSentence = itemDto.ExampleSentence?.Trim(),
                ExampleTranslation = itemDto.ExampleTranslation?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _homophoneRepo.AddAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(group);
    }

    public async Task<HomophoneGroupDto> UpdateGroupAsync(UpdateHomophoneGroupDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var group = await _homophoneRepo.GetWithItemsAsync(dto.Id, cancellationToken);
        if (group == null)
        {
            throw new KeyNotFoundException($"Homophone group with ID {dto.Id} was not found.");
        }

        group.Title = dto.Title.Trim();
        group.PhoneticIpa = dto.PhoneticIpa?.Trim() ?? string.Empty;
        group.Type = dto.Type;
        group.DifficultyLevel = dto.DifficultyLevel;
        group.ExamTag = dto.ExamTag;
        group.DifferentiationTip = dto.DifferentiationTip?.Trim();
        group.Notes = dto.Notes?.Trim();
        group.UpdatedAt = DateTime.UtcNow;

        // Clear existing items and repopulate
        _homophoneRepo.RemoveItems(group.Items);
        group.Items.Clear();

        foreach (var itemDto in dto.Items)
        {
            if (string.IsNullOrWhiteSpace(itemDto.Term)) continue;

            group.Items.Add(new HomophoneItem
            {
                HomophoneGroupId = group.Id,
                Term = itemDto.Term.Trim(),
                NormalizedTerm = itemDto.Term.Trim().ToLowerInvariant(),
                PartOfSpeech = itemDto.PartOfSpeech,
                MeaningVi = itemDto.MeaningVi?.Trim() ?? string.Empty,
                MeaningEn = itemDto.MeaningEn?.Trim(),
                PhoneticSpecific = itemDto.PhoneticSpecific?.Trim(),
                ExampleSentence = itemDto.ExampleSentence?.Trim(),
                ExampleTranslation = itemDto.ExampleTranslation?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _homophoneRepo.UpdateAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(group);
    }

    public async Task<bool> DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _homophoneRepo.GetWithItemsAsync(id, cancellationToken);
        if (group == null) return false;

        _homophoneRepo.RemoveItems(group.Items);
        await _homophoneRepo.DeleteAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<HomophoneGroupDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        return await GetAllGroupsAsync(searchTerm: query, cancellationToken: cancellationToken);
    }

    private static IReadOnlyList<HomophoneGroupDto> RankHomophones(IEnumerable<HomophoneGroup> groups, string searchTerm)
    {
        return groups
            .Select(g =>
            {
                var itemsText = g.Items != null
                    ? string.Join(" ", g.Items.Select(i => $"{i.Term} {i.MeaningVi} {i.ExampleSentence}"))
                    : string.Empty;
                var score = Lexora.Domain.Common.SearchTextHelper.ScoreMatch(
                    searchTerm,
                    g.Title,
                    (itemsText, 0.95),
                    (g.PhoneticIpa, 0.90),
                    (g.DifferentiationTip, 0.75),
                    (g.Notes, 0.65));
                return (Group: g, Score: score);
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => MapToDto(x.Group))
            .ToList();
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _homophoneRepo.CountAsync(cancellationToken: cancellationToken);
    }

    private static HomophoneGroupDto MapToDto(HomophoneGroup group) => group.ToDto();
}
