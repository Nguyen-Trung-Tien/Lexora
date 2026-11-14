using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Presentation.ViewModels;

public partial class RoadmapViewModel
{
    [RelayCommand]
    public async Task GenerateRoadmapAsync(CancellationToken cancellationToken = default)
    {
        await _actionLock.WaitAsync(cancellationToken);
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            var request = new GenerateRoadmapRequestDto(
                string.Empty,
                SelectedExamTarget,
                StartingScore,
                TargetScore,
                DurationWeeks);

            ActiveRoadmap = await _roadmapService.GenerateAdaptiveRoadmapAsync(request, cancellationToken);

            VisualNodes.Clear();
            if (ActiveRoadmap != null)
            {
                foreach (var milestone in ActiveRoadmap.Milestones)
                {
                    foreach (var node in milestone.Nodes)
                    {
                        VisualNodes.Add(node);
                    }
                }

                SelectedNode = ActiveRoadmap.NextActiveNode ?? VisualNodes.FirstOrDefault();
                IsNodeDetailOpen = true;

                // Save active id
                var prefs = _preferencesService.GetPreferences();
                _ = _preferencesService.SavePreferencesAsync(prefs with 
                { 
                    LastActiveRoadmapId = ActiveRoadmap.Id,
                    RoadmapViewMode = 0 
                });
            }

            OnPropertyChanged(nameof(VisualNodes));
            OnPropertyChanged(nameof(NextActiveNode));
            OnPropertyChanged(nameof(HasNextActiveNode));
            OnPropertyChanged(nameof(Milestones));

            var all = await _roadmapService.GetAllRoadmapsAsync(cancellationToken);
            AllRoadmaps.Clear();
            foreach (var r in all)
            {
                AllRoadmaps.Add(r);
            }

            SelectedViewMode = 0; // Switch to Track View immediately
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"[RoadmapViewModel] GenerateRoadmapAsync error: {ex}");
            App.LogCrash("RoadmapViewModel.GenerateRoadmapAsync", ex);
        }
        finally
        {
            IsLoading = false;
            _actionLock.Release();
        }
    }

    [RelayCommand]
    public async Task SwitchRoadmapAsync(Guid roadmapId)
    {
        await _actionLock.WaitAsync();
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            await _roadmapService.SetActiveRoadmapAsync(roadmapId);
            await LoadActiveRoadmapCoreAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            _actionLock.Release();
        }
    }

    [RelayCommand]
    public async Task DeleteRoadmapAsync(Guid roadmapId)
    {
        await _actionLock.WaitAsync();
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            await _roadmapService.DeleteRoadmapAsync(roadmapId);
            await LoadActiveRoadmapCoreAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            _actionLock.Release();
        }
    }

    [RelayCommand]
    public async Task ResetRoadmapProgressAsync(Guid? roadmapId = null)
    {
        var targetId = roadmapId ?? ActiveRoadmap?.Id;
        if (!targetId.HasValue) return;

        await _actionLock.WaitAsync();
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            var updated = await _roadmapService.ResetRoadmapProgressAsync(targetId.Value);

            if (updated != null && ActiveRoadmap?.Id == targetId.Value)
            {
                ActiveRoadmap = updated;
                SelectedNode = updated.Milestones
                    .OrderBy(m => m.OrderIndex)
                    .SelectMany(m => m.Nodes.OrderBy(n => n.OrderIndex))
                    .FirstOrDefault(n => n.Status == NodeStatus.Available)
                    ?? updated.Milestones.SelectMany(m => m.Nodes).FirstOrDefault();
            }

            var all = await _roadmapService.GetAllRoadmapsAsync();
            AllRoadmaps.Clear();
            foreach (var r in all)
            {
                AllRoadmaps.Add(r);
            }

            StatusMessage = _localizationService.GetString("Roadmap_ResetProgressSuccess");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoadmapViewModel] ResetRoadmapProgressAsync error: {ex}");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            _actionLock.Release();
        }
    }

    [RelayCommand]
    public async Task ClearInactiveRoadmapsAsync()
    {
        await _actionLock.WaitAsync();
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            int removedCount = await _roadmapService.ClearInactiveRoadmapsAsync();

            var all = await _roadmapService.GetAllRoadmapsAsync();
            AllRoadmaps.Clear();
            foreach (var r in all)
            {
                AllRoadmaps.Add(r);
            }

            StatusMessage = string.Format(_localizationService.GetString("Roadmap_ClearInactiveSuccess"), removedCount);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoadmapViewModel] ClearInactiveRoadmapsAsync error: {ex}");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            _actionLock.Release();
        }
    }

    [RelayCommand]
    public async Task ClearAllRoadmapsAsync()
    {
        await _actionLock.WaitAsync();
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            await _roadmapService.ClearAllRoadmapsAsync();

            ActiveRoadmap = null;
            SelectedNode = null;
            AllRoadmaps.Clear();

            var prefs = _preferencesService.GetPreferences();
            await _preferencesService.SavePreferencesAsync(prefs with { LastActiveRoadmapId = null });

            StatusMessage = _localizationService.GetString("Roadmap_ClearAllSuccess");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoadmapViewModel] ClearAllRoadmapsAsync error: {ex}");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            _actionLock.Release();
        }
    }

    [RelayCommand]
    public async Task QuickCreateRoadmapAsync(string preset)
    {
        (ExamTarget target, double start, double goal, int weeks) = preset switch
        {
            "TOEIC_750" => (ExamTarget.Toeic, 450.0, 750.0, 12),
            "IELTS_65" => (ExamTarget.Ielts, 5.0, 6.5, 16),
            "VSTEP_B2" => (ExamTarget.Vstep, 4.0, 6.5, 10),
            _ => (ExamTarget.Toeic, 450.0, 750.0, 12)
        };

        SelectedExamTarget = target;
        StartingScore = start;
        TargetScore = goal;
        DurationWeeks = weeks;

        await GenerateRoadmapAsync();
    }
}
