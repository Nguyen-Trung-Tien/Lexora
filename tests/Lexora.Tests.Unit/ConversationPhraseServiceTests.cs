using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services.Phrases;
using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class ConversationPhraseServiceTests
{
    private readonly Mock<IConversationPhraseRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ConversationPhraseService _service;

    public ConversationPhraseServiceTests()
    {
        _service = new ConversationPhraseService(_repoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAndSave_WhenValidDtoProvided()
    {
        // Arrange
        var dto = new CreateConversationPhraseDto
        {
            Category = "Greetings & Introductions",
            ContextTag = "First Meeting",
            PromptSentence = "Nice to meet you!",
            PromptTranslation = "Rất vui được gặp bạn!",
            SampleResponse = "Nice to meet you too!",
            ResponseTranslation = "Tôi cũng rất vui được gặp bạn!",
            AdditionalSuggestions = "• Casual alternative: 'Good to see you!'",
            CefrLevel = CefrLevel.A1,
            IsFavorite = true
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Category.Should().Be("Greetings & Introductions");
        result.PromptSentence.Should().Be("Nice to meet you!");
        result.PromptTranslation.Should().Be("Rất vui được gặp bạn!");
        result.SampleResponse.Should().Be("Nice to meet you too!");
        result.ResponseTranslation.Should().Be("Tôi cũng rất vui được gặp bạn!");
        result.IsFavorite.Should().BeTrue();
        result.CefrLevel.Should().Be(CefrLevel.A1);

        _repoMock.Verify(r => r.AddAsync(It.Is<ConversationPhrase>(p =>
            p.PromptSentence == "Nice to meet you!" &&
            p.Category == "Greetings & Introductions"), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", "Dịch", "Response", "Dịch", "Category")]
    [InlineData("Prompt", "", "Response", "Dịch", "Category")]
    [InlineData("Prompt", "Dịch", "", "Dịch", "Category")]
    [InlineData("Prompt", "Dịch", "Response", "", "Category")]
    [InlineData("Prompt", "Dịch", "Response", "Dịch", "")]
    public async Task CreateAsync_ShouldThrowArgumentException_WhenRequiredFieldIsBlank(
        string prompt, string promptTrans, string response, string responseTrans, string category)
    {
        // Arrange
        var dto = new CreateConversationPhraseDto
        {
            PromptSentence = prompt,
            PromptTranslation = promptTrans,
            SampleResponse = response,
            ResponseTranslation = responseTrans,
            Category = category
        };

        // Act & Assert
        var act = () => _service.CreateAsync(dto);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateFieldsAndSave_WhenPhraseExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new ConversationPhrase
        {
            Id = id,
            Category = "Old Category",
            PromptSentence = "Old Prompt",
            PromptTranslation = "Dịch cũ",
            SampleResponse = "Old Response",
            ResponseTranslation = "Dịch cũ",
            CefrLevel = CefrLevel.A2,
            IsFavorite = false
        };

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var updateDto = new UpdateConversationPhraseDto
        {
            Id = id,
            Category = "Workplace & Office",
            ContextTag = "Updated Tag",
            PromptSentence = "Could you review this report?",
            PromptTranslation = "Bạn xem qua bản báo cáo này được không?",
            SampleResponse = "Sure, I will take a look this afternoon.",
            ResponseTranslation = "Chắc chắn rồi, tôi sẽ xem vào chiều nay.",
            AdditionalSuggestions = "• Note: Formal tone.",
            CefrLevel = CefrLevel.B2,
            IsFavorite = true
        };

        // Act
        var result = await _service.UpdateAsync(updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Category.Should().Be("Workplace & Office");
        result.PromptSentence.Should().Be("Could you review this report?");
        result.CefrLevel.Should().Be(CefrLevel.B2);
        result.IsFavorite.Should().BeTrue();

        _repoMock.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowKeyNotFoundException_WhenPhraseNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationPhrase?)null);

        var updateDto = new UpdateConversationPhraseDto
        {
            Id = id,
            Category = "Category",
            PromptSentence = "Prompt",
            PromptTranslation = "Trans",
            SampleResponse = "Response",
            ResponseTranslation = "Trans"
        };

        // Act & Assert
        var act = () => _service.UpdateAsync(updateDto);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallRepoDeleteAndSave_WhenPhraseExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new ConversationPhrase { Id = id };
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        await _service.DeleteAsync(id);

        // Assert
        _repoMock.Verify(r => r.DeleteAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowKeyNotFoundException_WhenPhraseNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationPhrase?)null);

        // Act & Assert
        var act = () => _service.DeleteAsync(id);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ToggleFavoriteAsync_ShouldInvertStatusAndSave()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new ConversationPhrase { Id = id, IsFavorite = false };
        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _service.ToggleFavoriteAsync(id);

        // Assert
        result.Should().BeTrue();
        existing.IsFavorite.Should().BeTrue();

        _repoMock.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPhrasesAsync_ShouldReturnFilteredResults()
    {
        // Arrange
        var list = new List<ConversationPhrase>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Category = "Workplace & Office",
                PromptSentence = "Any updates on the project?",
                PromptTranslation = "Có cập nhật gì về dự án không?",
                SampleResponse = "We are on track.",
                ResponseTranslation = "Chúng tôi đang đúng tiến độ.",
                CefrLevel = CefrLevel.B1
            }
        };

        _repoMock.Setup(r => r.GetFilteredAsync("Workplace & Office", CefrLevel.B1, null, "project", It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var filter = new ConversationPhraseFilterDto
        {
            Category = "Workplace & Office",
            CefrLevel = CefrLevel.B1,
            SearchQuery = "project"
        };

        // Act
        var results = await _service.GetPhrasesAsync(filter);

        // Assert
        results.Should().HaveCount(1);
        results[0].PromptSentence.Should().Be("Any updates on the project?");
    }

    [Fact]
    public async Task GetQuickTemplatesAsync_ShouldReturnDefaultTemplates()
    {
        // Act
        var templates = await _service.GetQuickTemplatesAsync();

        // Assert
        templates.Should().NotBeEmpty();
        templates.Should().Contain(t => t.CategoryTag == "Formal");
        templates.Should().Contain(t => t.CategoryTag == "Casual");
    }

    [Fact]
    public async Task GenerateEnhancedSuggestionsAsync_ShouldReturnStructuredString()
    {
        // Act
        var suggestion = await _service.GenerateEnhancedSuggestionsAsync(
            "Hello there", "Hi how are you", "Daily Conversation");

        // Assert
        suggestion.Should().NotBeNullOrWhiteSpace();
        suggestion.Should().Contain("Ngữ cảnh áp dụng: Daily Conversation");
        suggestion.Should().Contain("Formal variation");
        suggestion.Should().Contain("Casual variation");
    }
}
