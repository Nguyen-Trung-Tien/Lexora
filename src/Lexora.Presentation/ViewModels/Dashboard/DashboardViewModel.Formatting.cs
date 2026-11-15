using Lexora.Domain.Enums;

namespace Lexora.Presentation.ViewModels;

public partial class DashboardViewModel
{
    private void UpdateGreeting()
    {
        int hour = DateTime.Now.Hour;
        if (hour < 12)
            GreetingText = _localizationService.GetString("Dashboard_GreetingMorning");
        else if (hour < 18)
            GreetingText = _localizationService.GetString("Dashboard_GreetingAfternoon");
        else
            GreetingText = _localizationService.GetString("Dashboard_GreetingEvening");
    }

    private void UpdateAdviceDisplay()
    {
        if (DailyAdvice == null) return;
        bool isVi = _localizationService.CurrentLanguage.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        AdviceTitle = isVi ? DailyAdvice.AdviceTitleVi : DailyAdvice.AdviceTitleEn;
        AdviceDescription = isVi ? DailyAdvice.AdviceDescriptionVi : DailyAdvice.AdviceDescriptionEn;
        AdviceButtonText = isVi ? DailyAdvice.ActionButtonTextVi : DailyAdvice.ActionButtonTextEn;
    }

    private string GetLocalizedDayLabel(string dayLabel)
    {
        return dayLabel switch
        {
            "Today" => _localizationService.GetString("Day_Today"),
            "Tomorrow" => _localizationService.GetString("Day_Tomorrow"),
            "Mon" => _localizationService.GetString("Day_Mon"),
            "Tue" => _localizationService.GetString("Day_Tue"),
            "Wed" => _localizationService.GetString("Day_Wed"),
            "Thu" => _localizationService.GetString("Day_Thu"),
            "Fri" => _localizationService.GetString("Day_Fri"),
            "Sat" => _localizationService.GetString("Day_Sat"),
            "Sun" => _localizationService.GetString("Day_Sun"),
            _ => dayLabel
        };
    }

    private string GetLocalizedStatusName(LearningStatus status) => status switch
    {
        LearningStatus.Mastered => _localizationService.GetString("Status_Mastered"),
        LearningStatus.Remembered => _localizationService.GetString("Status_Remembered"),
        LearningStatus.Learning => _localizationService.GetString("Status_Learning"),
        LearningStatus.Review => _localizationService.GetString("Status_ReviewDue"),
        LearningStatus.NotRemembered => _localizationService.GetString("Status_NotRemembered"),
        LearningStatus.New => _localizationService.GetString("Status_New"),
        _ => status.ToString()
    };

    private string GetLocalizedExamName(ExamType exam) => exam switch
    {
        ExamType.Toeic => _localizationService.GetString("Exam_Name_Toeic"),
        ExamType.Vstep => _localizationService.GetString("Exam_Name_Vstep"),
        ExamType.Ielts => _localizationService.GetString("Exam_Name_Ielts"),
        _ => _localizationService.GetString("Exam_Name_General")
    };

    private string GetLocalizedExamDescription(ExamType exam) => exam switch
    {
        ExamType.Toeic => _localizationService.GetString("Exam_Target_Toeic"),
        ExamType.Vstep => _localizationService.GetString("Exam_Target_Vstep"),
        ExamType.Ielts => _localizationService.GetString("Exam_Target_Ielts"),
        _ => _localizationService.GetString("Exam_Target_General")
    };

    private void UpdateLocalizedStatusAndExams()
    {
        if (_rawStatusBreakdown != null)
        {
            StatusBreakdown.Clear();
            foreach (var s in _rawStatusBreakdown)
            {
                StatusBreakdown.Add(s with { DisplayName = GetLocalizedStatusName(s.Status) });
            }
        }

        if (_rawExamProgress != null)
        {
            ExamProgressList.Clear();
            foreach (var ep in _rawExamProgress)
            {
                ExamProgressList.Add(ep with
                {
                    CustomExamName = GetLocalizedExamName(ep.Exam),
                    CustomTargetDescription = GetLocalizedExamDescription(ep.Exam)
                });
            }
        }
    }

    private void UpdateForecastDisplay()
    {
        ForecastDaysList.Clear();
        if (WorkloadForecast != null)
        {
            TotalForecastNext7Days = WorkloadForecast.TotalDueNext7Days;
            foreach (var d in WorkloadForecast.Days)
            {
                ForecastDaysList.Add(d with { DayLabel = GetLocalizedDayLabel(d.DayLabel) });
            }
        }
    }

    private void UpdateLocalizedDistributions()
    {
        if (_rawCefrDistribution != null)
        {
            CefrDistributionList.Clear();
            foreach (var c in _rawCefrDistribution) CefrDistributionList.Add(c);
        }

        if (_rawPosDistribution != null)
        {
            PosDistributionList.Clear();
            foreach (var p in _rawPosDistribution) PosDistributionList.Add(p);
        }
    }

    public void RefreshVisuals()
    {
        UpdateLocalizedHeatmap();
        OnPropertyChanged(nameof(StudyHeatmapList));
    }

    private void UpdateLocalizedHeatmap()
    {
        if (_rawHeatmap == null) return;
        bool isVi = _localizationService.CurrentLanguage.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        StudyHeatmapList.Clear();
        foreach (var h in _rawHeatmap)
        {
            var tooltip = isVi
                ? $"{h.Date:dd/MM/yyyy}: {h.ReviewsCount} lượt ôn ({h.CorrectCount} chính xác)"
                : $"{h.Date:dd/MM/yyyy}: {h.ReviewsCount} reviews ({h.CorrectCount} correct)";
            StudyHeatmapList.Add(h with { CustomTooltip = tooltip });
        }
    }

    private void UpdateLocalizedRecentSessions()
    {
        if (_rawRecentSessions == null) return;
        bool isVi = _localizationService.CurrentLanguage.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        RecentSessionsList.Clear();
        foreach (var rs in _rawRecentSessions)
        {
            var duration = rs.CompletedAt.HasValue
                ? $"{(rs.CompletedAt.Value - rs.StartedAt).Minutes:D2}:{(rs.CompletedAt.Value - rs.StartedAt).Seconds:D2}"
                : (isVi ? "Đang diễn ra" : "In progress");
            RecentSessionsList.Add(rs with { CustomDurationText = duration });
        }
        HasRecentSessions = RecentSessionsList.Count > 0;
    }

    private void UpdateIntelligenceDisplay()
    {
        if (IntelligenceReport == null) return;
        bool isVi = _localizationService.CurrentLanguage.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

        var slot = IntelligenceReport.CognitiveTimeEfficiency.FirstOrDefault(s => s.IsPeakPerformanceSlot)
                   ?? IntelligenceReport.CognitiveTimeEfficiency.FirstOrDefault();
        if (slot != null)
        {
            string slotName = isVi ? slot.SlotNameVi : slot.SlotNameEn;
            string accSuffix = _localizationService.GetString("LearningIntel_AccuracySuffix");
            PeakEfficiencyDisplay = $"{slotName} ({slot.AccuracyRate:F0}% {accSuffix})";
        }
        else
        {
            PeakEfficiencyDisplay = _localizationService.GetString("LearningIntel_InsufficientData");
        }

        var vel = IntelligenceReport.WeeklyVelocity.LastOrDefault();
        if (vel != null)
        {
            var format = _localizationService.GetString("LearningIntel_VelocityFormat");
            VelocitySummaryDisplay = string.Format(format, vel.NetGrowth, vel.WordsLearned, vel.WordsMastered);
        }
        else
        {
            VelocitySummaryDisplay = _localizationService.GetString("LearningIntel_VelocityZero");
        }

        if (IntelligenceReport.ExamReadiness.Count > 0)
        {
            var ex = IntelligenceReport.ExamReadiness[0];
            ExamReadinessDisplay = $"{ex.ExamTitle}: {ex.ReadinessScore:F0}% ({ex.TargetScoreRange})";
        }
        else
        {
            ExamReadinessDisplay = _localizationService.GetString("LearningIntel_Evaluating");
        }

        MemoryHealthList.Clear();
        foreach (var seg in IntelligenceReport.MemoryHealthBreakdown)
        {
            MemoryHealthList.Add(seg with
            {
                CustomTitle = isVi ? seg.TitleVi : seg.TitleEn,
                CustomDescription = isVi ? seg.DescriptionVi : seg.DescriptionEn
            });
        }

        HighImpactRecommendation = isVi
            ? IntelligenceReport.HighImpactRecommendationVi
            : IntelligenceReport.HighImpactRecommendationEn;

        RetentionCurveList.Clear();
        foreach (var pt in IntelligenceReport.EmpiricalVsTheoreticalRetention)
        {
            string label = isVi
                ? (pt.IntervalDays <= 30 ? $"Ngày {pt.IntervalDays}" : "60 ngày+")
                : (pt.IntervalDays <= 30 ? $"Day {pt.IntervalDays}" : "60d+");
            string wordCount = isVi ? $"{pt.WordCount} từ" : $"{pt.WordCount} words";
            RetentionCurveList.Add(pt with
            {
                CustomLabel = label,
                CustomWordCountDisplay = wordCount
            });
        }

        ExamPredictionsList.Clear();
        if (IntelligenceReport.PredictedExamScores != null)
        {
            string confFormat = _localizationService.GetString("LearningIntel_ConfidenceFormat");
            string masteryFormat = _localizationService.GetString("LearningIntel_MasteryFormat");
            string daysFormat = _localizationService.GetString("LearningIntel_DaysToMilestoneFormat");
            string reachedLabel = _localizationService.GetString("LearningIntel_MilestoneReached");

            foreach (var pred in IntelligenceReport.PredictedExamScores)
            {
                string milestoneDisplay = pred.EstimatedDaysToNextMilestone <= 0
                    ? reachedLabel
                    : string.Format(daysFormat, pred.EstimatedDaysToNextMilestone);

                ExamPredictionsList.Add(pred with
                {
                    CustomRationale = isVi ? pred.RationaleVi : pred.RationaleEn,
                    CustomConfidenceDisplay = string.Format(confFormat, pred.ConfidencePercent),
                    CustomMasteryDisplay = string.Format(masteryFormat, pred.MasteryRatioPercent),
                    CustomMilestoneDisplay = milestoneDisplay
                });
            }
        }
    }
}
