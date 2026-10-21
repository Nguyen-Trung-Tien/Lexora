using FluentAssertions;
using Lexora.Application;
using Lexora.Application.Interfaces;
using Lexora.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class DependencyInjectionResolutionTests
{
    [Fact]
    public void AddApplication_ShouldResolveServices_WithoutCircularDependency()
    {
        // Arrange
        var services = new ServiceCollection();

        // Mock Infrastructure repositories
        services.AddScoped(_ => new Mock<IVocabularyRepository>().Object);
        services.AddScoped(_ => new Mock<IReviewRepository>().Object);
        services.AddScoped(_ => new Mock<IGrammarRepository>().Object);
        services.AddScoped(_ => new Mock<ILearningStorageRepository>().Object);
        services.AddScoped(_ => new Mock<IHomophoneRepository>().Object);
        services.AddScoped(_ => new Mock<IUnitOfWork>().Object);
        services.AddSingleton(_ => new Mock<IVoiceProvider>().Object);

        // Register application services
        services.AddApplication();

        var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = serviceProvider.CreateScope();

        // Act & Assert: All key services must resolve without throwing CircularDependency / InvalidOperationException
        var vocabService = scope.ServiceProvider.GetRequiredService<IVocabularyService>();
        vocabService.Should().NotBeNull();

        var vocabSuggestionService = scope.ServiceProvider.GetRequiredService<IVocabularySuggestionService>();
        vocabSuggestionService.Should().NotBeNull();

        var grammarService = scope.ServiceProvider.GetRequiredService<IGrammarService>();
        grammarService.Should().NotBeNull();

        var grammarSuggestionService = scope.ServiceProvider.GetRequiredService<IGrammarSuggestionService>();
        grammarSuggestionService.Should().NotBeNull();

        var reviewService = scope.ServiceProvider.GetRequiredService<IReviewService>();
        reviewService.Should().NotBeNull();

        var testService = scope.ServiceProvider.GetRequiredService<ITestService>();
        testService.Should().NotBeNull();

        var learningStorageService = scope.ServiceProvider.GetRequiredService<ILearningStorageService>();
        learningStorageService.Should().NotBeNull();

        var homophoneService = scope.ServiceProvider.GetRequiredService<IHomophoneService>();
        homophoneService.Should().NotBeNull();

        var homophoneExportImportService = scope.ServiceProvider.GetRequiredService<IHomophoneExportImportService>();
        homophoneExportImportService.Should().NotBeNull();

        var statsService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
        statsService.Should().NotBeNull();

        var fsrsEngine = scope.ServiceProvider.GetRequiredService<Lexora.Application.SpacedRepetition.IFsrsSpacedRepetitionEngine>();
        fsrsEngine.Should().NotBeNull();
    }
}
