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
    public void NewlyAddedKeys_MustReturnAccurateTranslations(string key, string expectedVi, string expectedEn)
    {
        _loc.SetLanguage("vi-VN");
        _loc.GetString(key).Should().Be(expectedVi);

        _loc.SetLanguage("en-US");
        _loc.GetString(key).Should().Be(expectedEn);
    }
}
