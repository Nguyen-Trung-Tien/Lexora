using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IStudyPlannerService
{
    Task<StudyGoalDto> GetUserGoalAsync(CancellationToken cancellationToken = default);
    Task<StudyGoalDto> SaveUserGoalAsync(StudyGoalDto goal, CancellationToken cancellationToken = default);
    Task<DailyCurriculumDto> GetTodayCurriculumAsync(CancellationToken cancellationToken = default);
    Task<DailyCurriculumDto> RecordTaskCompletionAsync(CurriculumTaskType taskType, int completedDelta = 1, CancellationToken cancellationToken = default);
}
