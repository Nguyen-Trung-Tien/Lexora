using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface ITestService
{
    Task<TestSessionDto> GenerateTestAsync(TestConfigurationDto config, CancellationToken cancellationToken = default);
    Task<TestResultDto> SubmitTestAsync(Guid sessionId, List<TestAnswerSubmissionDto> answers, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WordDto>> GetWeakWordsAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task<TestSessionDto?> GetActiveTestSessionAsync(CancellationToken cancellationToken = default);
    Task CancelActiveTestSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
