using FluentAssertions;
using Lexora.Domain.Entities.MemoryTips;
using Lexora.Domain.Enums;
using Lexora.Infrastructure.Data;
using Lexora.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexora.Tests.Integration;

public class MemoryTipRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LexoraDbContext _context;
    private readonly MemoryTipRepository _repo;

    public MemoryTipRepositoryTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LexoraDbContext(options);
        var initializer = new SqliteDatabaseInitializer(_context, NullLogger<SqliteDatabaseInitializer>.Instance);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _repo = new MemoryTipRepository(_context);
    }

    [Fact]
    public async Task DatabaseInitializer_ShouldSeedInitialMemoryTips()
    {
        // Act
        var tips = await _repo.GetAllAsync();

        // Assert
        tips.Should().NotBeEmpty();
        tips.Count.Should().BeGreaterThanOrEqualTo(7);
        tips.Should().Contain(t => t.Title.Contains("OpSASCOMP"));
        tips.Should().Contain(t => t.Title.Contains("FANBOYS"));
        tips.Should().Contain(t => t.Title.Contains("SINCE vs FOR"));
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_ShouldPersistAndRetrieveAccurately()
    {
        // Arrange
        var newTip = new MemoryTip
        {
            Title = "Custom Mnemonic Hook",
            MnemonicHook = "A hook to remember",
            Explanation = "Detailed reasoning",
            Example = "An example sentence",
            CommonTrap = "A classic trap",
            Category = TipCategory.Expressions,
            Technique = MnemonicTechnique.VisualImagery,
            ExamTarget = ExamTarget.Vstep,
            IsPinned = true,
            Tags = "Expressions, VSTEP"
        };

        // Act
        await _repo.AddAsync(newTip);
        await _context.SaveChangesAsync();

        var fetched = await _repo.GetByIdAsync(newTip.Id);

        // Assert
        fetched.Should().NotBeNull();
        fetched!.Title.Should().Be("Custom Mnemonic Hook");
        fetched.MnemonicHook.Should().Be("A hook to remember");
        fetched.Explanation.Should().Be("Detailed reasoning");
        fetched.Category.Should().Be(TipCategory.Expressions);
        fetched.Technique.Should().Be(MnemonicTechnique.VisualImagery);
        fetched.ExamTarget.Should().Be(ExamTarget.Vstep);
        fetched.IsPinned.Should().BeTrue();
    }

    [Fact]
    public async Task GetFilteredAsync_ShouldFilterBySearchAndCategory()
    {
        // Act: Filter by Grammar category
        var grammarTips = await _repo.GetFilteredAsync(category: TipCategory.Grammar);

        // Assert
        grammarTips.Should().NotBeEmpty();
        grammarTips.Should().OnlyContain(t => t.Category == TipCategory.Grammar);

        // Act: Filter by Search query
        var searchResults = await _repo.GetFilteredAsync(search: "FANBOYS");
        searchResults.Should().NotBeEmpty();
        searchResults.Should().Contain(t => t.MnemonicHook.Contains("FANBOYS"));
    }

    [Fact]
    public async Task GetFilteredAsync_ShouldReturnPinnedTipsFirst()
    {
        // Act
        var allTips = await _repo.GetFilteredAsync();

        // Assert
        allTips.Should().NotBeEmpty();
        bool seenUnpinned = false;
        foreach (var tip in allTips)
        {
            if (!tip.IsPinned)
            {
                seenUnpinned = true;
            }
            else if (seenUnpinned)
            {
                // If we see a pinned tip AFTER seeing an unpinned tip, sorting invariant is violated
                Assert.Fail("Pinned tip was found after unpinned tips in sorting order.");
            }
        }
    }

    [Fact]
    public async Task GetTagsAsync_ShouldReturnDistinctSortedTags()
    {
        // Act
        var tags = await _repo.GetTagsAsync();

        // Assert
        tags.Should().NotBeEmpty();
        tags.Should().Contain("Grammar");
        tags.Should().Contain("TOEIC");
        tags.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldPaginateAndReturnCorrectCount()
    {
        // Act: Page 1 with pageSize 3
        var (page1Items, totalCount) = await _repo.GetPagedAsync(1, 3);

        // Assert
        totalCount.Should().BeGreaterThanOrEqualTo(7);
        page1Items.Should().HaveCount(3);

        // Act: Page 2 with pageSize 3
        var (page2Items, _) = await _repo.GetPagedAsync(2, 3);
        page2Items.Should().HaveCount(3);
        page2Items.Select(x => x.Id).Should().NotIntersectWith(page1Items.Select(x => x.Id));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
