using FluentAssertions;
using Lexora.Application.DTOs.MemoryTips;
using Lexora.Application.Services.MemoryTips;
using Lexora.Domain.Entities.MemoryTips;
using Lexora.Domain.Enums;
using Lexora.Domain.Exceptions;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class MemoryTipServiceTests
{
    private readonly Mock<IMemoryTipRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly MemoryTipService _service;

    public MemoryTipServiceTests()
    {
        _service = new MemoryTipService(_repoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistAndReturnDto_WhenValidRequestProvided()
    {
        // Arrange
        var request = new CreateMemoryTipRequest
        {
            Title = "Thứ tự tính từ OpSASCOMP",
            MnemonicHook = "Ông Sáu Ăn Súp Cua Ông Mập Phì",
            Explanation = "Opinion, Size, Age, Shape, Color, Origin, Material, Purpose",
            Example = "A lovely small old round black wooden table.",
            CommonTrap = "Đề thi hay đổi chỗ Size và Color.",
            Category = TipCategory.Grammar,
            Technique = MnemonicTechnique.Acrostic,
            ExamTarget = ExamTarget.Toeic,
            IsPinned = true,
            Tags = "Adjectives, Grammar"
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Title.Should().Be(request.Title);
        result.MnemonicHook.Should().Be(request.MnemonicHook);
        result.Explanation.Should().Be(request.Explanation);
        result.Example.Should().Be(request.Example);
        result.CommonTrap.Should().Be(request.CommonTrap);
        result.Category.Should().Be(TipCategory.Grammar);
        result.Technique.Should().Be(MnemonicTechnique.Acrostic);
        result.ExamTarget.Should().Be(ExamTarget.Toeic);
        result.IsPinned.Should().BeTrue();
        result.Tags.Should().Be("Adjectives, Grammar");

        _repoMock.Verify(r => r.AddAsync(It.Is<MemoryTip>(t =>
            t.Title == request.Title &&
            t.MnemonicHook == request.MnemonicHook &&
            t.IsPinned == true), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", "Hook", "Explanation")]
    [InlineData("   ", "Hook", "Explanation")]
    [InlineData(null, "Hook", "Explanation")]
    public async Task CreateAsync_ShouldThrowDomainValidationException_WhenTitleIsInvalid(string? title, string hook, string explanation)
    {
        var request = new CreateMemoryTipRequest
        {
            Title = title!,
            MnemonicHook = hook,
            Explanation = explanation
        };

        var act = async () => await _service.CreateAsync(request);

        await act.Should().ThrowAsync<DomainValidationException>()
            .Where(e => e.PropertyName == "title");
    }

    [Theory]
    [InlineData("Title", "", "Explanation")]
    [InlineData("Title", "   ", "Explanation")]
    [InlineData("Title", null, "Explanation")]
    public async Task CreateAsync_ShouldThrowDomainValidationException_WhenMnemonicHookIsInvalid(string title, string? hook, string explanation)
    {
        var request = new CreateMemoryTipRequest
        {
            Title = title,
            MnemonicHook = hook!,
            Explanation = explanation
        };

        var act = async () => await _service.CreateAsync(request);

        await act.Should().ThrowAsync<DomainValidationException>()
            .Where(e => e.PropertyName == "mnemonicHook");
    }

    [Theory]
    [InlineData("Title", "Hook", "")]
    [InlineData("Title", "Hook", "   ")]
    [InlineData("Title", "Hook", null)]
    public async Task CreateAsync_ShouldThrowDomainValidationException_WhenExplanationIsInvalid(string title, string hook, string? explanation)
    {
        var request = new CreateMemoryTipRequest
        {
            Title = title,
            MnemonicHook = hook,
            Explanation = explanation!
        };

        var act = async () => await _service.CreateAsync(request);

        await act.Should().ThrowAsync<DomainValidationException>()
            .Where(e => e.PropertyName == "explanation");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAndReturnDto_WhenTipExists()
    {
        // Arrange
        var tipId = Guid.NewGuid();
        var existing = new MemoryTip
        {
            Id = tipId,
            Title = "Old Title",
            MnemonicHook = "Old Hook",
            Explanation = "Old Explanation",
            Category = TipCategory.Vocabulary,
            Technique = MnemonicTechnique.RhymeAndRhythm,
            IsPinned = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _repoMock.Setup(r => r.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new UpdateMemoryTipRequest
        {
            Id = tipId,
            Title = "Updated Title",
            MnemonicHook = "Updated Hook",
            Explanation = "Updated Explanation",
            Category = TipCategory.ExamStrategy,
            Technique = MnemonicTechnique.EliminationRule,
            ExamTarget = ExamTarget.Ielts,
            IsPinned = true,
            CommonTrap = "Beware of synonym traps."
        };

        // Act
        var result = await _service.UpdateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
        result.MnemonicHook.Should().Be("Updated Hook");
        result.Explanation.Should().Be("Updated Explanation");
        result.Category.Should().Be(TipCategory.ExamStrategy);
        result.Technique.Should().Be(MnemonicTechnique.EliminationRule);
        result.ExamTarget.Should().Be(ExamTarget.Ielts);
        result.IsPinned.Should().BeTrue();
        result.CommonTrap.Should().Be("Beware of synonym traps.");

        _repoMock.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowEntityNotFoundException_WhenTipDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemoryTip?)null);

        var request = new UpdateMemoryTipRequest
        {
            Id = nonExistentId,
            Title = "Title",
            MnemonicHook = "Hook",
            Explanation = "Explanation"
        };

        var act = async () => await _service.UpdateAsync(request);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteAndReturnTrue_WhenTipExists()
    {
        var tipId = Guid.NewGuid();
        var existing = new MemoryTip
        {
            Id = tipId,
            Title = "To Delete",
            MnemonicHook = "Hook",
            Explanation = "Explanation"
        };

        _repoMock.Setup(r => r.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.DeleteAsync(tipId);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.DeleteAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenTipDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemoryTip?)null);

        var result = await _service.DeleteAsync(nonExistentId);

        result.Should().BeFalse();
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<MemoryTip>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TogglePinAsync_ShouldInvertIsPinned_WhenTipExists()
    {
        var tipId = Guid.NewGuid();
        var existing = new MemoryTip
        {
            Id = tipId,
            Title = "Pinnable Tip",
            MnemonicHook = "Hook",
            Explanation = "Explanation",
            IsPinned = false
        };

        _repoMock.Setup(r => r.GetByIdAsync(tipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act 1: Toggle from false -> true
        var result1 = await _service.TogglePinAsync(tipId);
        result1.IsPinned.Should().BeTrue();
        existing.IsPinned.Should().BeTrue();

        // Act 2: Toggle from true -> false
        var result2 = await _service.TogglePinAsync(tipId);
        result2.IsPinned.Should().BeFalse();
        existing.IsPinned.Should().BeFalse();

        _repoMock.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldComputeAccurateAggregations()
    {
        var tips = new List<MemoryTip>
        {
            new() { Title = "T1", MnemonicHook = "H1", Explanation = "E1", Category = TipCategory.Vocabulary, IsPinned = true, CommonTrap = "Trap 1" },
            new() { Title = "T2", MnemonicHook = "H2", Explanation = "E2", Category = TipCategory.Grammar, IsPinned = false, CommonTrap = null },
            new() { Title = "T3", MnemonicHook = "H3", Explanation = "E3", Category = TipCategory.ExamStrategy, IsPinned = true, CommonTrap = "Trap 2" },
            new() { Title = "T4", MnemonicHook = "H4", Explanation = "E4", Category = TipCategory.Vocabulary, IsPinned = false, CommonTrap = "" }
        };

        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tips);

        var stats = await _service.GetStatisticsAsync();

        stats.Should().NotBeNull();
        stats.TotalCount.Should().Be(4);
        stats.PinnedCount.Should().Be(2);
        stats.TrapCount.Should().Be(2);
        stats.VocabularyCount.Should().Be(2);
        stats.GrammarCount.Should().Be(1);
        stats.ExamStrategyCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedTipsAsync_ShouldCalculatePaginationAndMapDtos()
    {
        // Arrange
        var mockItems = new List<MemoryTip>
        {
            new() { Title = "Tip 1", MnemonicHook = "H1", Explanation = "E1" },
            new() { Title = "Tip 2", MnemonicHook = "H2", Explanation = "E2" }
        };

        _repoMock.Setup(r => r.GetPagedAsync(
            1, 6, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((mockItems, 15));

        // Act
        var result = await _service.GetPagedTipsAsync(1, 6);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(6);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }
}
