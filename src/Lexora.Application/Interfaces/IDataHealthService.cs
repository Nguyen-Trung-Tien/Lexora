using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IDataHealthService
{
    Task<DatabaseHealthReportDto> RunDiagnosticsAsync(CancellationToken cancellationToken = default);
    Task<DataRepairResultDto> RepairAndOptimizeAsync(bool vacuumDatabase = true, bool clearAudioCache = false, CancellationToken cancellationToken = default);
    Task<long> ClearAudioCacheAsync(CancellationToken cancellationToken = default);
}
