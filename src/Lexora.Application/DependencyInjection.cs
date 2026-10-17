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
        services.AddScoped<IStudyPlannerService, StudyPlannerService>();
        services.AddScoped<IVocabularyKnowledgeGraphService, VocabularyKnowledgeGraphService>();
        services.AddScoped<IAdaptiveQuizService, AdaptiveQuizService>();
        services.AddScoped<IPronunciationListeningLabService, PronunciationListeningLabService>();
        services.AddSingleton<IPronunciationVoiceResolver, PronunciationVoiceResolver>();

        return services;
    }
}
