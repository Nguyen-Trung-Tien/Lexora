using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Infrastructure.Data;
using Lexora.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexora.Tests.Integration;

public class RoadmapPersistenceIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LexoraDbContext _context;
    private readonly RoadmapRepository _roadmapRepo;
    private readonly VocabularyRepository _vocabRepo;
    private readonly GrammarRepository _grammarRepo;
    private readonly UnitOfWork _unitOfWork;
    private readonly RoadmapService _roadmapService;

    public RoadmapPersistenceIntegrationTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LexoraDbContext(options);
        var initializer = new SqliteDatabaseInitializer(_context, NullLogger<SqliteDatabaseInitializer>.Instance);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _roadmapRepo = new RoadmapRepository(_context);
        _vocabRepo = new VocabularyRepository(_context);
        _grammarRepo = new GrammarRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
        _roadmapService = new RoadmapService(_roadmapRepo, _unitOfWork, _vocabRepo, _grammarRepo);
    }

    [Fact]
    public async Task GenerateRoadmap_ThenReloadActiveRoadmap_ShouldPersistAndReturnRoadmap()
    {
        // 1. Generate roadmap
        var request = new GenerateRoadmapRequestDto(
            string.Empty,
            ExamTarget.Toeic,
            450,
            750,
            12);

        var generated = await _roadmapService.GenerateAdaptiveRoadmapAsync(request);
        generated.Should().NotBeNull();
        generated.Milestones.Should().NotBeEmpty();

        // 2. Query directly from repository
        var active = await _roadmapRepo.GetActiveRoadmapWithDetailsAsync();
        active.Should().NotBeNull();
        active!.IsActive.Should().BeTrue();
        active.Milestones.Should().HaveCount(generated.Milestones.Count);
        active.Milestones.SelectMany(m => m.Nodes).Should().HaveCount(11);

        // 3. Sync roadmap progress
        var synced = await _roadmapService.SyncRoadmapProgressAsync();
        synced.Should().NotBeNull();
        synced!.Milestones.Should().HaveCount(generated.Milestones.Count);

        // 4. Update progress
        var firstNode = synced.Milestones.First().Nodes.First();
        var updateResult = await _roadmapService.UpdateNodeProgressAsync(firstNode.Id, 10, 2.5);
        updateResult.Should().BeTrue();

        var afterUpdate = await _roadmapService.GetActiveRoadmapAsync();
        afterUpdate.Should().NotBeNull();
        var updatedNode = afterUpdate!.Milestones.First().Nodes.First();
        updatedNode.CompletedItemCount.Should().Be(10);
    }

    [Fact]
    public async Task GenerateRoadmap_WhenActiveRoadmapAlreadyTrackedInContext_ShouldNotThrowTrackingConflict()
    {
        // 1. Generate first roadmap
        var request1 = new GenerateRoadmapRequestDto(string.Empty, ExamTarget.Toeic, 450, 750, 12);
        var first = await _roadmapService.GenerateAdaptiveRoadmapAsync(request1);
        first.Should().NotBeNull();

        // 2. Query active roadmap so it is actively tracked in DbContext identity map
        var trackedActive = await _roadmapRepo.GetActiveRoadmapWithDetailsAsync();
        trackedActive.Should().NotBeNull();
        trackedActive!.IsActive.Should().BeTrue();

        // 3. Generate a second roadmap in the SAME context
        var request2 = new GenerateRoadmapRequestDto(string.Empty, ExamTarget.Ielts, 5.0, 6.5, 16);
        var second = await _roadmapService.GenerateAdaptiveRoadmapAsync(request2);
        second.Should().NotBeNull();
        second.Target.Should().Be(ExamTarget.Ielts);

        // 4. Verify old roadmap is now deactivated and new one is active
        var activeNow = await _roadmapRepo.GetActiveRoadmapWithDetailsAsync();
        activeNow.Should().NotBeNull();
        activeNow!.Id.Should().Be(second.Id);
        activeNow.IsActive.Should().BeTrue();

        var old = await _roadmapRepo.GetRoadmapWithDetailsAsync(first.Id);
        old.Should().NotBeNull();
        old!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ResetRoadmapProgress_ShouldResetAllNodesAndMilestonesBackToInitialState()
    {
        // 1. Generate roadmap
        var request = new GenerateRoadmapRequestDto(string.Empty, ExamTarget.Toeic, 450, 750, 12);
        var generated = await _roadmapService.GenerateAdaptiveRoadmapAsync(request);
        generated.Should().NotBeNull();

        // 2. Advance progress on the first 2 nodes
        var firstNode = generated.Milestones.First().Nodes.First();
        await _roadmapService.UpdateNodeProgressAsync(firstNode.Id, 100, 2.8);

        var afterFirst = await _roadmapService.GetActiveRoadmapAsync();
        var secondNode = afterFirst!.Milestones.First().Nodes.Skip(1).First();
        await _roadmapService.UpdateNodeProgressAsync(secondNode.Id, 50, 2.6);

        // Verify progress is advanced
        var beforeReset = await _roadmapService.GetActiveRoadmapAsync();
        beforeReset!.OverallProgress.Should().BeGreaterThan(0);

        // 3. Reset roadmap progress
        var resetResult = await _roadmapService.ResetRoadmapProgressAsync(generated.Id);
        resetResult.Should().NotBeNull();
        resetResult!.OverallProgress.Should().Be(0);

        // 4. Verify in DB: all nodes have CompletedItemCount == 0, only node 0 is Available, all others Locked
        var fromDb = await _roadmapRepo.GetRoadmapWithDetailsAsync(generated.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Milestones.Should().OnlyContain(m => !m.IsCompleted);

        var allNodes = fromDb.Milestones
            .OrderBy(m => m.OrderIndex)
            .SelectMany(m => m.Nodes.OrderBy(n => n.OrderIndex))
            .ToList();

        allNodes.Should().OnlyContain(n => n.CompletedItemCount == 0);
        allNodes.First().Status.Should().Be(NodeStatus.Available);
        allNodes.Skip(1).Should().OnlyContain(n => n.Status == NodeStatus.Locked);
        allNodes.Should().OnlyContain(n => n.EaseFactorWeight == 2.5);
    }

    [Fact]
    public async Task ClearInactiveRoadmaps_ShouldDeleteOnlyInactiveRoadmapsAndPreserveActive()
    {
        // 1. Generate two roadmaps
        var req1 = new GenerateRoadmapRequestDto("Roadmap 1", ExamTarget.Toeic, 450, 750, 12);
        var first = await _roadmapService.GenerateAdaptiveRoadmapAsync(req1);

        var req2 = new GenerateRoadmapRequestDto("Roadmap 2", ExamTarget.Ielts, 5.0, 6.5, 16);
        var second = await _roadmapService.GenerateAdaptiveRoadmapAsync(req2);

        // req2 is active, req1 is inactive
        var allBefore = await _roadmapService.GetAllRoadmapsAsync();
        allBefore.Should().HaveCount(2);

        // 2. Clear inactive roadmaps
        int removedCount = await _roadmapService.ClearInactiveRoadmapsAsync();
        removedCount.Should().Be(1);

        // 3. Verify only active roadmap remains
        var allAfter = await _roadmapService.GetAllRoadmapsAsync();
        allAfter.Should().HaveCount(1);
        allAfter.First().Id.Should().Be(second.Id);
        allAfter.First().IsActive.Should().BeTrue();

        var deleted = await _roadmapRepo.GetRoadmapWithDetailsAsync(first.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task ClearAllRoadmaps_ShouldDeleteAllRoadmapsInHistory()
    {
        // 1. Generate roadmaps
        var req1 = new GenerateRoadmapRequestDto("Roadmap 1", ExamTarget.Toeic, 450, 750, 12);
        await _roadmapService.GenerateAdaptiveRoadmapAsync(req1);

        var req2 = new GenerateRoadmapRequestDto("Roadmap 2", ExamTarget.Ielts, 5.0, 6.5, 16);
        await _roadmapService.GenerateAdaptiveRoadmapAsync(req2);

        // 2. Clear all roadmaps
        bool cleared = await _roadmapService.ClearAllRoadmapsAsync();
        cleared.Should().BeTrue();

        // 3. Verify DB has 0 roadmaps
        var all = await _roadmapService.GetAllRoadmapsAsync();
        all.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}

