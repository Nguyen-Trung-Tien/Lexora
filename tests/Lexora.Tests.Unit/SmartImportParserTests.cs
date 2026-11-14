using FluentAssertions;
using Lexora.Application.Services;
using Lexora.Domain.Enums;
using Xunit;

namespace Lexora.Tests.Unit;

public class SmartImportParserTests
{
    [Fact]
    public void StripMarkdownCodeFence_WithMarkdownFence_ShouldReturnCleanJson()
    {
        // Arrange
        var markdown = "```json\n[{\"term\": \"resilient\"}]\n```";

        // Act
        var result = SmartImportParser.StripMarkdownCodeFence(markdown);

        // Assert
        result.Should().Be("[{\"term\": \"resilient\"}]");
    }

    [Fact]
    public void StripMarkdownCodeFence_WithPlainText_ShouldReturnOriginalTrimmed()
    {
        // Arrange
        var plain = "  [{\"term\": \"resilient\"}]  ";

        // Act
        var result = SmartImportParser.StripMarkdownCodeFence(plain);

        // Assert
        result.Should().Be("[{\"term\": \"resilient\"}]");
    }

    [Fact]
    public void DeserializeWordsJson_WithDirectArray_ShouldReturnList()
    {
        // Arrange
        var json = """
        [
            {
                "term": "perseverance",
                "phoneticUs": "/ˌpɜːr.səˈvɪr.əns/",
                "level": "C1",
                "primaryPartOfSpeech": "noun",
                "meanings": [
                    {
                        "partOfSpeech": "noun",
                        "definitionVi": "Sự kiên trì, bền chí",
                        "definitionEn": "Persistence in doing something despite difficulty"
                    }
                ]
            }
        ]
        """;

        // Act
        var result = SmartImportParser.DeserializeWordsJson(json);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].Term.Should().Be("perseverance");
        result[0].Level.Should().Be(CefrLevel.C1);
        result[0].PrimaryPartOfSpeech.Should().Be(PartOfSpeech.Noun);
        result[0].Meanings.Should().HaveCount(1);
        result[0].Meanings[0].DefinitionVi.Should().Be("Sự kiên trì, bền chí");
    }

    [Fact]
    public void DeserializeWordsJson_WithWrappedWordsObject_ShouldReturnList()
    {
        // Arrange
        var json = """
        {
            "words": [
                {
                    "term": "adaptable",
                    "level": "B2"
                }
            ]
        }
        """;

        // Act
        var result = SmartImportParser.DeserializeWordsJson(json);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].Term.Should().Be("adaptable");
        result[0].Level.Should().Be(CefrLevel.B2);
    }

    [Theory]
    [InlineData("noun", PartOfSpeech.Noun)]
    [InlineData("n", PartOfSpeech.Noun)]
    [InlineData("danh từ", PartOfSpeech.Noun)]
    [InlineData("verb", PartOfSpeech.Verb)]
    [InlineData("v", PartOfSpeech.Verb)]
    [InlineData("động từ", PartOfSpeech.Verb)]
    [InlineData("adjective", PartOfSpeech.Adjective)]
    [InlineData("adj", PartOfSpeech.Adjective)]
    [InlineData("tính từ", PartOfSpeech.Adjective)]
    [InlineData("adverb", PartOfSpeech.Adverb)]
    [InlineData("adv", PartOfSpeech.Adverb)]
    [InlineData("phó từ", PartOfSpeech.Adverb)]
    [InlineData("idiom", PartOfSpeech.Idiom)]
    [InlineData("phrasal verb", PartOfSpeech.PhrasalVerb)]
    public void TryParsePartOfSpeech_ShouldParseCorrectly(string input, PartOfSpeech expected)
    {
        // Act
        var success = SmartImportParser.TryParsePartOfSpeech(input, out var pos);

        // Assert
        success.Should().BeTrue();
        pos.Should().Be(expected);
    }

    [Theory]
    [InlineData("A1", CefrLevel.A1)]
    [InlineData("a2", CefrLevel.A2)]
    [InlineData("B1", CefrLevel.B1)]
    [InlineData("b2", CefrLevel.B2)]
    [InlineData("C1", CefrLevel.C1)]
    [InlineData("c2", CefrLevel.C2)]
    public void ParseCefrLevel_ShouldParseCorrectly(string input, CefrLevel expected)
    {
        // Act
        var level = SmartImportParser.ParseCefrLevel(input);

        // Assert
        level.Should().Be(expected);
    }

    [Fact]
    public void ParseSmartTxtLine_WithTabSeparatedColumns_ShouldExtractFields()
    {
        // Arrange
        var line = "meticulous\t/məˈtɪk.jə.ləs/\tadj\tB2\tTỉ mỉ, kỹ lưỡng\tVery careful and with great attention to detail\tHe was meticulous about keeping records.";

        // Act
        var row = SmartImportParser.ParseSmartTxtLine(line);

        // Assert
        row.Should().NotBeNull();
        row!.Term.Should().Be("meticulous");
        row.PhoneticUk.Should().Be("/məˈtɪk.jə.ləs/");
        row.PartOfSpeech.Should().Be(PartOfSpeech.Adjective);
        row.Level.Should().Be(CefrLevel.B2);
        row.DefinitionVi.Should().Be("Tỉ mỉ, kỹ lưỡng");
        row.Examples.Should().Contain("He was meticulous about keeping records.");
    }
}
