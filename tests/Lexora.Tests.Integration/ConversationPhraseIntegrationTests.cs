using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services.Phrases;
using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;
using Lexora.Infrastructure.Data;
using Lexora.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexora.Tests.Integration;

public class ConversationPhraseIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LexoraDbContext _context;
    private readonly ConversationPhraseRepository _repo;
    private readonly UnitOfWork _unitOfWork;
    private readonly ConversationPhraseService _service;

    public ConversationPhraseIntegrationTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LexoraDbContext(options);
        var initializer = new SqliteDatabaseInitializer(_context, NullLogger<SqliteDatabaseInitializer>.Instance);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _repo = new ConversationPhraseRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
        _service = new ConversationPhraseService(_repo, _unitOfWork);
    }

    [Fact]
    public async Task DatabaseInitializer_ShouldSeedInitialConversationPhrases()
    {
        // Act
        var phrases = await _repo.GetAllAsync();

        // Assert
        phrases.Should().NotBeEmpty();
        phrases.Count.Should().BeGreaterThanOrEqualTo(10);
        phrases.Should().Contain(p => p.Category == "Greetings & Introductions");
        phrases.Should().Contain(p => p.Category == "Workplace & Office");
        phrases.Should().Contain(p => p.Category == "Job Interviews");
    }

    [Fact]
    public async Task CreatePhrase_ViaService_ShouldPersistInSqliteAndBeSearchable()
    {
        // Arrange
        var createDto = new CreateConversationPhraseDto
        {
            Category = "Customer Support",
            ContextTag = "Refund Request",
            PromptSentence = "How can I request a refund for this subscription?",
            PromptTranslation = "Tôi có thể yêu cầu hoàn tiền cho gói đăng ký này như thế nào?",
            SampleResponse = "You can submit a ticket via our billing portal, and we will process it within 24 hours.",
            ResponseTranslation = "Bạn có thể gửi yêu cầu qua cổng thanh toán của chúng tôi và chúng tôi sẽ xử lý trong vòng 24 giờ.",
            AdditionalSuggestions = "• Formal note: State policy clearly.",
            CefrLevel = CefrLevel.B2,
            IsFavorite = true
        };

        // Act
        var created = await _service.CreateAsync(createDto);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();

        // Query directly via repository
        var retrieved = await _repo.GetByIdAsync(created.Id);
        retrieved.Should().NotBeNull();
        retrieved!.PromptSentence.Should().Be(createDto.PromptSentence);
        retrieved.Category.Should().Be("Customer Support");
        retrieved.IsFavorite.Should().BeTrue();
        retrieved.NormalizedPrompt.Should().Be(createDto.PromptSentence.Trim().ToLowerInvariant());

        // Search by term
        var searchResults = await _repo.GetFilteredAsync(search: "refund");
        searchResults.Should().Contain(p => p.Id == created.Id);
    }

    [Fact]
    public async Task UpdatePhrase_ViaService_ShouldUpdateAndPersist()
    {
        // Arrange
        var createDto = new CreateConversationPhraseDto
        {
            Category = "Socializing",
            ContextTag = "Weekend Plans",
            PromptSentence = "Do you have any plans for the weekend?",
            PromptTranslation = "Bạn có kế hoạch gì cho cuối tuần chưa?",
            SampleResponse = "I might go hiking with some colleagues.",
            ResponseTranslation = "Tôi có thể sẽ đi leo núi với vài đồng nghiệp.",
            CefrLevel = CefrLevel.A2
        };

        var created = await _service.CreateAsync(createDto);

        var updateDto = new UpdateConversationPhraseDto
        {
            Id = created.Id,
            Category = "Socializing",
            ContextTag = "Weekend Plans",
            PromptSentence = "Any exciting plans for the upcoming weekend?",
            PromptTranslation = "Có kế hoạch gì hào hứng cho cuối tuần sắp tới không?",
            SampleResponse = "I'm heading out for a camping trip in the mountains.",
            ResponseTranslation = "Tôi sẽ đi cắm trại ở vùng núi.",
            AdditionalSuggestions = "• Casual phrasing: 'Up to anything this weekend?'",
            CefrLevel = CefrLevel.B1,
            IsFavorite = true
        };

        // Act
        var updated = await _service.UpdateAsync(updateDto);

        // Assert
        updated.PromptSentence.Should().Be("Any exciting plans for the upcoming weekend?");
        updated.CefrLevel.Should().Be(CefrLevel.B1);
        updated.IsFavorite.Should().BeTrue();

        var inDb = await _repo.GetByIdAsync(created.Id);
        inDb.Should().NotBeNull();
        inDb!.PromptSentence.Should().Be("Any exciting plans for the upcoming weekend?");
        inDb.SampleResponse.Should().Be("I'm heading out for a camping trip in the mountains.");
    }

    [Fact]
    public async Task DeletePhrase_ViaService_ShouldRemoveFromDatabase()
    {
        // Arrange
        var createDto = new CreateConversationPhraseDto
        {
            Category = "Test Category",
            PromptSentence = "Temporary question?",
            PromptTranslation = "Câu hỏi tạm thời?",
            SampleResponse = "Temporary answer.",
            ResponseTranslation = "Câu trả lời tạm thời.",
            CefrLevel = CefrLevel.A1
        };

        var created = await _service.CreateAsync(createDto);

        // Act
        await _service.DeleteAsync(created.Id);

        // Assert
        var inDb = await _repo.GetByIdAsync(created.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnDistinctSeededCategories()
    {
        // Act
        var categories = await _repo.GetCategoriesAsync();

        // Assert
        categories.Should().NotBeEmpty();
        categories.Should().Contain("Greetings & Introductions");
        categories.Should().Contain("Workplace & Office");
        categories.Should().Contain("Travel & Directions");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
