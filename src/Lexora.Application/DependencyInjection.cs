using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Application.SpacedRepetition;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ISpacedRepetitionEngine, Sm2SpacedRepetitionEngine>();
        services.AddSingleton<IFsrsSpacedRepetitionEngine, FsrsSpacedRepetitionEngine>();
        services.AddScoped<IVocabularyService, VocabularyService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IGrammarService, GrammarService>();
        services.AddScoped<IExportImportService, ExportImportService>();
        services.AddScoped<IGrammarExportImportService, GrammarExportImportService>();
        services.AddScoped<ITestService, TestService>();
        services.AddScoped<ILearningStorageService, LearningStorageService>();
        services.AddSingleton<IFuzzySearchService, FuzzySearchService>();
        services.AddSingleton<ISmartSearchService, SmartSearchService>();
        services.AddSingleton<ISmartSearchCacheService, SmartSearchCacheService>();
        services.AddScoped<IVocabularySuggestionService, VocabularySuggestionService>();
        services.AddScoped<IGrammarSuggestionService, GrammarSuggestionService>();
        services.AddScoped<IStudyAdvisorService, StudyAdvisorService>();
        services.AddScoped<IStudyPlannerService>(sp => new StudyPlannerService(
            sp.GetRequiredService<Lexora.Domain.Interfaces.IReviewRepository>(),
            sp.GetRequiredService<Lexora.Domain.Interfaces.IVocabularyRepository>(),
            sp.GetRequiredService<Lexora.Domain.Interfaces.IGrammarRepository>(),
            null,
            sp.GetService<Lexora.Domain.Interfaces.IRoadmapRepository>()));
        services.AddScoped<IVocabularyKnowledgeGraphService, VocabularyKnowledgeGraphService>();
        services.AddScoped<IAdaptiveQuizService, AdaptiveQuizService>();
        services.AddScoped<IPronunciationListeningLabService, PronunciationListeningLabService>();
        services.AddSingleton<IPronunciationVoiceResolver, PronunciationVoiceResolver>();
        services.AddScoped<IHomophoneService, HomophoneService>();
        services.AddScoped<IHomophoneExportImportService, HomophoneExportImportService>();
        services.AddScoped<IKnowledgeGraphExportImportService, KnowledgeGraphExportImportService>();
        services.AddScoped<IRoadmapService, RoadmapService>();

        return services;
    }
}
