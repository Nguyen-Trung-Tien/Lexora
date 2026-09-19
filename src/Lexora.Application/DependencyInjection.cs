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
        services.AddScoped<IVocabularyService, VocabularyService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IGrammarService, GrammarService>();
        services.AddScoped<IExportImportService, ExportImportService>();
        services.AddScoped<IGrammarExportImportService, GrammarExportImportService>();
        services.AddScoped<ITestService, TestService>();
        services.AddSingleton<IFuzzySearchService, FuzzySearchService>();
        services.AddScoped<IStudyAdvisorService, StudyAdvisorService>();

        return services;
    }
}
