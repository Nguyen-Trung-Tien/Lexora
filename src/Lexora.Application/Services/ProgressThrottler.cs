namespace Lexora.Application.Services;

using System.Diagnostics;
using Lexora.Application.DTOs;

/// <summary>
/// Throttles IProgress.Report calls to avoid overwhelming UI dispatchers during large batch imports.
/// </summary>
public sealed class ProgressThrottler
{
    private readonly IProgress<ProgressReport>? _progress;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private int _lastPercent = -1;
    private readonly int _throttleMs;

    public ProgressThrottler(IProgress<ProgressReport>? progress, int throttleMs = 100)
    {
        _progress = progress;
        _throttleMs = throttleMs;
    }

    public void Report(int percent, int current, int total, string message, string? detail = null, bool force = false)
    {
        if (_progress == null) return;
        if (force || percent != _lastPercent || _stopwatch.ElapsedMilliseconds >= _throttleMs)
        {
            _lastPercent = percent;
            _stopwatch.Restart();
            _progress.Report(new ProgressReport(percent, current, total, message, detail));
        }
    }
}
