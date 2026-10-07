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

    [Fact]
    public async Task UpdateTopicAsync_WithValidDto_ShouldUpdateAndReturnDto()
    {
        // Arrange
        var topicId = Guid.NewGuid();
        var existingTopic = new Topic
        {
            Id = topicId,
            Name = "Old Name",
            Description = "Old Desc",
            IconGlyph = "\uE825",
            ColorHex = "#000000"
        };

        var updateDto = new TopicCreateOrUpdateDto(
            Name: "New Tech Topic",
            Description: "Updated technology vocabulary",
            IconGlyph: "\uE7F4",
            ColorHex: "#0078D4"
        );

        _mockVocabRepo.Setup(r => r.GetTopicByIdAsync(topicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTopic);
        _mockVocabRepo.Setup(r => r.GetTopicByNameAsync("New Tech Topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Topic?)null);

        // Act
        var result = await _vocabService.UpdateTopicAsync(topicId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Tech Topic");
        result.Description.Should().Be("Updated technology vocabulary");
        result.IconGlyph.Should().Be("\uE7F4");
        existingTopic.Name.Should().Be("New Tech Topic");
        existingTopic.Description.Should().Be("Updated technology vocabulary");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTopicAsync_WithGuidEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var updateDto = new TopicCreateOrUpdateDto("Tech");

        // Act
        var act = () => _vocabService.UpdateTopicAsync(Guid.Empty, updateDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*General Context*");
    }

    [Fact]
    public async Task UpdateTopicAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var topicId = Guid.NewGuid();
        var updateDto = new TopicCreateOrUpdateDto("   ");

        // Act
        var act = () => _vocabService.UpdateTopicAsync(topicId, updateDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Topic name cannot be empty*");
    }

    [Fact]
    public async Task UpdateTopicAsync_WithDuplicateNameFromOtherTopic_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var topicId = Guid.NewGuid();
        var otherTopicId = Guid.NewGuid();
        var existingTopic = new Topic { Id = topicId, Name = "Current Name" };
        var duplicateTopic = new Topic { Id = otherTopicId, Name = "Existing Name" };

        _mockVocabRepo.Setup(r => r.GetTopicByIdAsync(topicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTopic);
        _mockVocabRepo.Setup(r => r.GetTopicByNameAsync("Existing Name", It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicateTopic);

        var updateDto = new TopicCreateOrUpdateDto("Existing Name");

        // Act
        var act = () => _vocabService.UpdateTopicAsync(topicId, updateDto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task DeleteTopicAsync_WithGuidEmpty_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _vocabService.DeleteTopicAsync(Guid.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*General Context*");
    }

    [Fact]
    public async Task AssignWordsToTopicAsync_ShouldAddOnlyMissingAssociationsAndCommit()
    {
        // Arrange
        var topicId = Guid.NewGuid();
        var word1 = Guid.NewGuid();
        var word2 = Guid.NewGuid();

        _mockVocabRepo.Setup(r => r.GetTopicByIdAsync(topicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Topic { Id = topicId, Name = "Topic" });

        _mockVocabRepo.Setup(r => r.GetExistingWordTopicAssociationsAsync(topicId, It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { word1 });

        // Act
        await _vocabService.AssignWordsToTopicAsync(topicId, new[] { word1, word2 });

        // Assert
        _mockVocabRepo.Verify(r => r.AddWordTopics(
            It.Is<IEnumerable<WordTopic>>(items => items.Count() == 1 && items.First().WordId == word2 && items.First().TopicId == topicId)), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignWordsToTopicAsync_WithGuidEmpty_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _vocabService.AssignWordsToTopicAsync(Guid.Empty, new[] { Guid.NewGuid() });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*General Context*");
    }

    [Fact]
    public async Task UnassignWordsFromTopicAsync_ShouldCallRepoAndCommit()
    {
        // Arrange
        var topicId = Guid.NewGuid();
        var word1 = Guid.NewGuid();

        // Act
        await _vocabService.UnassignWordsFromTopicAsync(topicId, new[] { word1 });

        // Assert
        _mockVocabRepo.Verify(r => r.RemoveWordTopicAssociationsAsync(
            topicId,
            It.Is<IEnumerable<Guid>>(ids => ids.Contains(word1)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnassignWordsFromTopicAsync_WithGuidEmpty_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _vocabService.UnassignWordsFromTopicAsync(Guid.Empty, new[] { Guid.NewGuid() });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*General Context*");
    }

    [Fact]
    public async Task DeleteTopicsAsync_WithValidTopicIds_ShouldDeleteAllAndCommit()
    {
        // Arrange
        var topicId1 = Guid.NewGuid();
        var topicId2 = Guid.NewGuid();
        var topic1 = new Topic { Id = topicId1, Name = "Topic 1" };
        var topic2 = new Topic { Id = topicId2, Name = "Topic 2" };

        _mockVocabRepo.Setup(r => r.GetTopicByIdAsync(topicId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topic1);
        _mockVocabRepo.Setup(r => r.GetTopicByIdAsync(topicId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topic2);

        // Act
        var deletedCount = await _vocabService.DeleteTopicsAsync(new[] { topicId1, topicId2 });

        // Assert
        deletedCount.Should().Be(2);
        _mockVocabRepo.Verify(r => r.DeleteTopic(topic1), Times.Once);
        _mockVocabRepo.Verify(r => r.DeleteTopic(topic2), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTopicsAsync_WithGuidEmpty_ShouldIgnoreGuidEmpty()
    {
        // Arrange
        var topicId1 = Guid.NewGuid();
        var topic1 = new Topic { Id = topicId1, Name = "Topic 1" };

        _mockVocabRepo.Setup(r => r.GetTopicByIdAsync(topicId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topic1);

        // Act
        var deletedCount = await _vocabService.DeleteTopicsAsync(new[] { Guid.Empty, topicId1 });

        // Assert
        deletedCount.Should().Be(1);
        _mockVocabRepo.Verify(r => r.DeleteTopic(topic1), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTopicsAsync_WithNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _vocabService.DeleteTopicsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

