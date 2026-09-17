using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class TopicServiceTests
{
    private readonly Mock<IVocabularyRepository> _mockVocabRepo;
    private readonly Mock<IReviewRepository> _mockReviewRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly VocabularyService _vocabService;

    public TopicServiceTests()
    {
        _mockVocabRepo = new Mock<IVocabularyRepository>();
        _mockReviewRepo = new Mock<IReviewRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _vocabService = new VocabularyService(
            _mockVocabRepo.Object,
            _mockReviewRepo.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task CreateTopicAsync_WithValidDto_ShouldAddTopicAndReturnDto()
    {
        // Arrange
        var dto = new TopicCreateOrUpdateDto(
            Name: "Business & Management",
            Description: "Essential vocabulary for corporate and business meetings.",
            IconGlyph: "\uE825",
            ColorHex: "#0078D4"
        );

        _mockVocabRepo.Setup(r => r.GetTopicByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Topic?)null);

        _mockVocabRepo.Setup(r => r.AddTopicAsync(It.IsAny<Topic>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _vocabService.CreateTopicAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Business & Management");
        result.Description.Should().Be("Essential vocabulary for corporate and business meetings.");
        result.IconGlyph.Should().Be("\uE825");
        result.ColorHex.Should().Be("#0078D4");

        _mockVocabRepo.Verify(r => r.AddTopicAsync(It.Is<Topic>(t => t.Name == "Business & Management"), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTopicAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var dto = new TopicCreateOrUpdateDto(Name: "   ");

        // Act
        var act = () => _vocabService.CreateTopicAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Topic name cannot be empty*");
    }

    [Fact]
    public async Task CreateTopicAsync_WithDuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var dto = new TopicCreateOrUpdateDto(Name: "Travel & Hospitality");

        _mockVocabRepo.Setup(r => r.GetTopicByNameAsync("Travel & Hospitality", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Topic { Name = "Travel & Hospitality" });

        // Act
        var act = () => _vocabService.CreateTopicAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task DeleteTopicAsync_WithExistingTopic_ShouldDeleteAndSave()
    {
        // Arrange
        var topicId = Guid.NewGuid();
        var existingTopic = new Topic { Id = topicId, Name = "Obsolete Topic" };

        _mockVocabRepo.Setup(r => r.GetTopicByIdAsync(topicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTopic);

        // Act
        await _vocabService.DeleteTopicAsync(topicId);

        // Assert
        _mockVocabRepo.Verify(r => r.DeleteTopic(existingTopic), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTopicAsync_WithNonExistentTopic_ShouldNotCallDeleteOrSave()
    {
        // Arrange
        var topicId = Guid.NewGuid();
        _mockVocabRepo.Setup(r => r.GetTopicByIdAsync(topicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Topic?)null);

        // Act
        await _vocabService.DeleteTopicAsync(topicId);

        // Assert
        _mockVocabRepo.Verify(r => r.DeleteTopic(It.IsAny<Topic>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetWordsByIdsAsync_ShouldReturnMappedDtos()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var words = new List<Word>
        {
            new() { Id = id1, Term = "Apple", NormalizedTerm = "apple" },
            new() { Id = id2, Term = "Banana", NormalizedTerm = "banana" }
        };

        _mockVocabRepo.Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(words);

        // Act
        var result = await _vocabService.GetWordsByIdsAsync(new[] { id1, id2 });

        // Assert
        result.Should().HaveCount(2);
        result.Select(w => w.Term).Should().Contain(new[] { "Apple", "Banana" });
        _mockVocabRepo.Verify(r => r.GetByIdsWithDetailsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(id1) && ids.Contains(id2)), It.IsAny<CancellationToken>()), Times.Once);
    }
}
