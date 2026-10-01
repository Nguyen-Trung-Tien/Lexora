using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Enums;
using Xunit;

namespace Lexora.Tests.Unit;

public class VocabularySuggestionServiceTests
{
    private readonly VocabularySuggestionService _sut = new();

    private static WordDto CreateWord(
        Guid id,
        string term,
        string defVi,
        string defEn = "",
        string explanation = "",
        List<string>? topics = null,
        string notes = "",
        PartOfSpeech pos = PartOfSpeech.Noun)
    {
        return new WordDto(
            Id: id,
            Term: term,
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B2,
            PrimaryPartOfSpeech: pos,
            Notes: notes,
            Meanings: new List<WordMeaningDto>
            {
                new(Guid.NewGuid(), pos, defVi, defEn, explanation, 0, new List<ExampleSentenceDto>())
            },
            Topics: topics ?? new List<string>(),
            ExamTags: new List<string>(),
            LearningStatus: LearningStatus.Learning,
            NextReviewAt: null,
            Confidence: 0.0,
            IntervalDays: 0,
            EaseFactor: 2.5,
            Lapses: 0,
            Repetitions: 0,
            TotalReviews: 0,
            RetentionRate: 0.0
        );
    }

    [Fact]
    public void BuildSuggestions_DetectsSynonyms_ViaDefinitionOverlap()
    {
        var targetId = Guid.NewGuid();
        var target = CreateWord(targetId, "huge", "khổng lồ, to lớn", "extremely large", notes: "Synonym: gigantic, immense");
        var synonymWord = CreateWord(Guid.NewGuid(), "gigantic", "to lớn khổng lồ", "extremely large in size");
        var unrelated = CreateWord(Guid.NewGuid(), "microscope", "kính hiển vi", "instrument for magnification");

        var result = _sut.GetSuggestions(target, new[] { target, synonymWord, unrelated });

        result.Synonyms.Should().NotBeEmpty();
        result.Synonyms.Should().Contain(s => s.Word.Term == "gigantic");
        result.Synonyms.Should().NotContain(s => s.Word.Term == "microscope");
    }

    [Fact]
    public void BuildSuggestions_DetectsAntonyms_ViaNotesOrDefinitions()
    {
        var targetId = Guid.NewGuid();
        var target = CreateWord(targetId, "increase", "tăng lên", "become greater in size or amount", notes: "Antonym: decrease");
        var antonymWord = CreateWord(Guid.NewGuid(), "decrease", "giảm đi, sụt giảm", "become smaller or fewer");

        var result = _sut.GetSuggestions(target, new[] { target, antonymWord });

        result.Antonyms.Should().NotBeEmpty();
        result.Antonyms.Should().Contain(s => s.Word.Term == "decrease");
    }

    [Fact]
    public void BuildSuggestions_DetectsWordFamily_ViaRootAffixes()
    {
        var targetId = Guid.NewGuid();
        var target = CreateWord(targetId, "careful", "cẩn thận, kỹ lưỡng", pos: PartOfSpeech.Adjective);
        var familyMember1 = CreateWord(Guid.NewGuid(), "careless", "bất cẩn, cẩu thả", pos: PartOfSpeech.Adjective);
        var familyMember2 = CreateWord(Guid.NewGuid(), "care", "sự quan tâm, cẩn trọng", pos: PartOfSpeech.Noun);
        var unrelated = CreateWord(Guid.NewGuid(), "career", "sự nghiệp, nghề nghiệp", pos: PartOfSpeech.Noun);

        var result = _sut.GetSuggestions(target, new[] { target, familyMember1, familyMember2, unrelated });

        result.WordFamily.Should().NotBeEmpty();
        result.WordFamily.Should().Contain(s => s.Word.Term == "careless" || s.Word.Term == "care");
        result.WordFamily.Should().NotContain(s => s.Word.Term == "career");
    }

    [Fact]
    public void BuildSuggestions_DetectsCollocations_WhenSubphraseMatches()
    {
        var targetId = Guid.NewGuid();
        var target = CreateWord(targetId, "decision", "quyết định");
        var phrase = CreateWord(Guid.NewGuid(), "make a decision", "đưa ra một quyết định quan trọng");
        var unrelated = CreateWord(Guid.NewGuid(), "decisive", "quyết đoán");

        var result = _sut.GetSuggestions(target, new[] { target, phrase, unrelated });

        result.Collocations.Should().NotBeEmpty();
        result.Collocations.Should().Contain(s => s.Word.Term == "make a decision");
    }

    [Fact]
    public void BuildSuggestions_SameTopic_GroupsContextualWords()
    {
        var targetId = Guid.NewGuid();
        var target = CreateWord(targetId, "stethoscope", "ống nghe bác sĩ", topics: new List<string> { "Medicine" });
        var medicineWord = CreateWord(Guid.NewGuid(), "surgeon", "bác sĩ phẫu thuật", topics: new List<string> { "Medicine" });
        var techWord = CreateWord(Guid.NewGuid(), "compiler", "trình biên dịch", topics: new List<string> { "Technology" });

        var result = _sut.GetSuggestions(target, new[] { target, medicineWord, techWord });

        result.SameTopic.Should().NotBeEmpty();
        result.SameTopic.Should().Contain(s => s.Word.Term == "surgeon");
        result.SameTopic.Should().NotContain(s => s.Word.Term == "compiler");
    }

    [Fact]
    public void BuildSuggestions_DiscardsIrrelevantOrWeakMatches()
    {
        var targetId = Guid.NewGuid();
        var target = CreateWord(targetId, "quantum", "lượng tử");
        var banana = CreateWord(Guid.NewGuid(), "banana", "quả chuối");
        var pencil = CreateWord(Guid.NewGuid(), "pencil", "bút chì");

        var result = _sut.GetSuggestions(target, new[] { target, banana, pencil });

        result.TotalSuggestionsCount.Should().Be(0, "No spurious suggestions should be generated for completely unrelated words");
    }
}
