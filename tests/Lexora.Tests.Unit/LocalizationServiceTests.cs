using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Lexora.Presentation.Services;
using Xunit;

namespace Lexora.Tests.Unit;

public class LocalizationServiceTests
{
    private readonly LocalizationService _loc = new();

    [Fact]
    public void DefaultLanguage_ShouldBeVietnamese()
    {
        _loc.CurrentLanguage.Should().Be("vi-VN");
    }

    [Fact]
    public void SetLanguage_ShouldUpdateCurrentLanguageAndRaiseEvent()
    {
        bool eventFired = false;
        _loc.LanguageChanged += (s, e) => eventFired = true;

        _loc.SetLanguage("en-US");

        _loc.CurrentLanguage.Should().Be("en-US");
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void GetString_WithExistingKey_ShouldReturnLocalizedText()
    {
        _loc.SetLanguage("vi-VN");
        var viTitle = _loc.GetString("AppTitle");
        viTitle.Should().NotBeNullOrWhiteSpace();

        _loc.SetLanguage("en-US");
        var enTitle = _loc.GetString("AppTitle");
        enTitle.Should().NotBeNullOrWhiteSpace();

        viTitle.Should().NotBe(enTitle);
    }

    [Fact]
    public void GetString_WithMissingKey_ShouldReturnKeyAsFallback()
    {
        const string unknownKey = "NonExistent_Test_Key_12345";
        var result = _loc.GetString(unknownKey);
        result.Should().Be(unknownKey);
    }

    [Fact]
    public void Indexer_ShouldMatchGetString()
    {
        _loc.SetLanguage("en-US");
        _loc["AppTitle"].Should().Be(_loc.GetString("AppTitle"));
    }

    [Fact]
    public void DictionaryParity_BothLocalesMustHaveExactSameKeys()
    {
        var field = typeof(LocalizationService).GetField("_resources", BindingFlags.NonPublic | BindingFlags.Instance);
        field.Should().NotBeNull();

        var resources = field!.GetValue(_loc) as Dictionary<string, Dictionary<string, string>>;
        resources.Should().NotBeNull();
        resources!.Should().ContainKey("vi-VN");
        resources!.Should().ContainKey("en-US");

        var viKeys = resources["vi-VN"].Keys.ToHashSet();
        var enKeys = resources["en-US"].Keys.ToHashSet();

        var missingInEn = viKeys.Except(enKeys).ToList();
        var missingInVi = enKeys.Except(viKeys).ToList();

        missingInEn.Should().BeEmpty("all vi-VN keys must be present in en-US dictionary");
        missingInVi.Should().BeEmpty("all en-US keys must be present in vi-VN dictionary");
        viKeys.Count.Should().Be(enKeys.Count);
        viKeys.Count.Should().BeGreaterThan(700, "the localization dictionary should contain all required application strings");
    }

    [Fact]
    public void FormatTokens_TokensMustMatchBetweenLocales()
    {
        var field = typeof(LocalizationService).GetField("_resources", BindingFlags.NonPublic | BindingFlags.Instance);
        var resources = (Dictionary<string, Dictionary<string, string>>)field!.GetValue(_loc)!;

        var viDict = resources["vi-VN"];
        var enDict = resources["en-US"];

        var tokenRegex = new Regex(@"\{(\d+)\}", RegexOptions.Compiled);
        var mismatches = new List<string>();

        foreach (var key in viDict.Keys)
        {
            var viVal = viDict[key];
            var enVal = enDict[key];

            var viTokens = tokenRegex.Matches(viVal).Select(m => m.Groups[1].Value).OrderBy(x => x).ToList();
            var enTokens = tokenRegex.Matches(enVal).Select(m => m.Groups[1].Value).OrderBy(x => x).ToList();

            if (!viTokens.SequenceEqual(enTokens))
            {
                mismatches.Add($"Key '{key}': vi=[{string.Join(",", viTokens)}] vs en=[{string.Join(",", enTokens)}]");
            }
        }

        mismatches.Should().BeEmpty("format string argument indices must match across locales to prevent FormatException");
    }

    [Fact]
    public void AllDictionaryEntries_MustNotBeNullOrWhiteSpace()
    {
        var field = typeof(LocalizationService).GetField("_resources", BindingFlags.NonPublic | BindingFlags.Instance);
        var resources = (Dictionary<string, Dictionary<string, string>>)field!.GetValue(_loc)!;

        foreach (var (locale, dict) in resources)
        {
            foreach (var (key, val) in dict)
            {
                val.Should().NotBeNullOrWhiteSpace($"key '{key}' in locale '{locale}' must have a valid non-empty string value");
            }
        }
    }

    [Fact]
    public void SwitchLanguage_MultipleTimes_TogglesCorrectlyAndFiresEvents()
    {
        int eventCount = 0;
        _loc.LanguageChanged += (s, e) => eventCount++;

        _loc.SetLanguage("vi-VN");
        _loc.CurrentLanguage.Should().Be("vi-VN");
        _loc.GetString("Action_Save").Should().Be("Lưu");

        _loc.SetLanguage("en-US");
        _loc.CurrentLanguage.Should().Be("en-US");
        _loc.GetString("Action_Save").Should().Be("Save");

        _loc.SetLanguage("vi-VN");
        _loc.CurrentLanguage.Should().Be("vi-VN");
        _loc.GetString("Action_Save").Should().Be("Lưu");

        _loc.SetLanguage("en-US");
        _loc.CurrentLanguage.Should().Be("en-US");
        _loc.GetString("Action_Save").Should().Be("Save");

        eventCount.Should().Be(4);
    }

    [Theory]
    [InlineData("Progress_DefaultPreparing", "Đang chuẩn bị dữ liệu...", "Preparing data...")]
    [InlineData("Progress_CompletedSummary", "Toàn bộ dữ liệu đã được xử lý an toàn.", "All data processed securely.")]
    [InlineData("Progress_StatImported", "Đã nhập", "Imported")]
    [InlineData("Progress_StatSkipped", "Bỏ qua", "Skipped")]
    [InlineData("Progress_StatError", "Lỗi", "Errors")]
    [InlineData("Storage_RetentionLabel", "Ghi nhớ:", "Retention:")]
    [InlineData("Storage_IntervalLabel", "Chu kỳ:", "Interval:")]
    [InlineData("Storage_MistakesLabel", "Lỗi:", "Mistakes:")]
    [InlineData("Storage_CorrectLabel", "• Đúng:", "• Correct:")]
    [InlineData("SmartReview_UrgentOverdue", "Cần ôn gấp (Quá hạn)", "Urgent Overdue")]
    [InlineData("SmartReview_HighForgettingRisk", "Nguy cơ quên cao (R < 70%)", "High Forgetting Risk (R < 70%)")]
    [InlineData("SmartReview_ExamHighYield", "Trọng tâm bài thi (High-Yield)", "Exam High-Yield")]
    [InlineData("SmartReview_FreshLearned", "Từ mới nạp (Củng cố)", "Recently Learned (Consolidation)")]
    [InlineData("SmartReview_LongTermConsolidation", "Khắc sâu dài hạn", "Long-Term Consolidation")]
    [InlineData("AdaptiveQuiz_FinalLevelFormat", "Trình độ ước tính: {0}", "Estimated Level: {0}")]
    [InlineData("Nav_Help", "Trung tâm hướng dẫn", "Help & Guide")]
    [InlineData("Help_HeroBadge", "TRUNG TÂM HƯỚNG DẪN & TRỢ GIÚP", "HELP & USER GUIDE HUB")]
    [InlineData("Help_Title", "Hướng dẫn sử dụng Lexora", "Lexora User Guide & Documentation")]
    [InlineData("Help_OfflineBadge", "Hoạt động Ngoại tuyến & SM-2/FSRS", "Offline-First & SM-2/FSRS")]
    [InlineData("Dashboard_AiIntelligenceBadge", "Trí tuệ học tập AI", "AI Intelligence")]
    [InlineData("Dashboard_AiHighImpactBadge", "Gợi ý AI trọng tâm", "AI High Impact")]
    [InlineData("Homophones_FilterAllTypes", "Tất cả loại", "All Types")]
    [InlineData("Homophones_EmptyList", "Không tìm thấy nhóm từ phù hợp", "No matching word groups found")]
    [InlineData("Homophones_ComparisonTitle", "Đối chiếu các từ thành phần", "Side-by-Side Word Comparison")]
    [InlineData("DataHealth_StatusAnalyzing", "Đang phân tích cấu trúc và bản ghi SQLite...", "Analyzing SQLite schema and records...")]
    [InlineData("DataHealth_DiagnosticsComplete", "Chẩn đoán hoàn tất. Điểm sức khỏe: {0}/100 ({1}).", "Diagnostics complete. Health score: {0}/100 ({1}).")]
    [InlineData("Help_ScrollLeft", "Cuộn danh mục sang trái", "Scroll categories left")]
    [InlineData("Help_ScrollRight", "Cuộn danh mục sang phải", "Scroll categories right")]
    [InlineData("Dashboard_Help_Title", "Trợ giúp: Bảng tổng quan", "Quick Help: Dashboard")]
    [InlineData("Review_Help_Title", "Trợ giúp: Ôn tập Spaced Repetition", "Quick Help: Spaced Repetition")]
    [InlineData("Homophones_Toast_GroupSaved", "Đã thêm nhóm từ '{0}' thành công!", "Homophone group '{0}' added successfully!")]
    [InlineData("KnowledgeGraph_ExportSuccess", "Đã xuất đồ thị tri thức sang '{0}' thành công!", "Knowledge graph exported to '{0}' successfully!")]
    [InlineData("SmartImport_Toast_Success", "Đã nhập thành công {0} mục dữ liệu!", "Successfully imported {0} items!")]
    [InlineData("Review_UndoSuccess", "Đã hoàn tác đánh giá thẻ ôn tập vừa rồi.", "Last card rating undone successfully.")]
    [InlineData("Action_Export", "Xuất tệp", "Export")]
    [InlineData("Action_Import", "Nhập tệp", "Import")]
    public void NewlyAddedKeys_MustReturnAccurateTranslations(string key, string expectedVi, string expectedEn)
    {
        _loc.SetLanguage("vi-VN");
        _loc.GetString(key).Should().Be(expectedVi);

        _loc.SetLanguage("en-US");
        _loc.GetString(key).Should().Be(expectedEn);
    }
}
