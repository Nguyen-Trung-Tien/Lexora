using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IAdaptiveQuizService
{
    Task<AdaptiveQuestionDto?> StartQuizAsync(AdaptiveQuizConfigDto config, CancellationToken cancellationToken = default);
    Task<AdaptiveAnswerEvaluationDto> SubmitAnswerAsync(Guid sessionId, int questionIndex, int selectedOptionIndex, int responseTimeMs, CancellationToken cancellationToken = default);
    Task<AdaptiveQuestionDto?> GetNextQuestionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<AdaptiveQuizSummaryDto> FinishQuizAsync(Guid sessionId, CancellationToken cancellationToken = default);
    bool HasActiveSession(Guid sessionId);
}
