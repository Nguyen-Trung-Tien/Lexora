using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class SmartImportWorkflowTests
{
    private readonly Mock<IVocabularyRepository> _mockVocabRepo = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly ExportImportService _service;

    public SmartImportWorkflowTests()
    {
        _service = new ExportImportService(_mockVocabRepo.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task AnalyzeSmartImportAsync_CsvFormat_ShouldDetectColumnsAndNormalize()
    {
        // Arrange
        var csvContent = "Word,Part of Speech,Meaning,Pronunciation,Example\n" +
                         "ephemeral,adjective,lasting for a very short time,/ɪˈfem.ər.əl/,Fashions are ephemeral.\n" +
                         "break down,phrasal verb,stop functioning,,The car broke down.\n" +
                         "by and large,phrase,on the whole; everything considered,,By and large, it was a good trip.";

        _mockVocabRepo.Setup(r => r.GetByTermAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        // Act
        var result = await _service.AnalyzeSmartImportAsync(csvContent, "test.csv");

        // Assert
        result.Should().NotBeNull();
        result.DetectedFormat.Should().Be(DataFormat.Csv);
        result.TotalParsed.Should().Be(3);
        result.ValidCount.Should().Be(3);
        result.DuplicateCount.Should().Be(0);

        var phraseItem = result.PreviewItems.FirstOrDefault(x => x.Term == "by and large");
        phraseItem.Should().NotBeNull();
        phraseItem!.PartOfSpeech.Should().Be(PartOfSpeech.Phrase);
        phraseItem.DefinitionVi.Should().Be("on the whole; everything considered");
    }

    [Fact]
    public async Task AnalyzeSmartImportAsync_WhenTermAlreadyExists_ShouldMarkAsDuplicate()
    {
        // Arrange
        var csvContent = "Term,Meaning\n" +
                         "ubiquitous,present everywhere\n" +
                         "serendipity,fortunate accident";

        var existingWord = new Word { Term = "ubiquitous", NormalizedTerm = "ubiquitous" };
        _mockVocabRepo.Setup(r => r.SearchAsync(
            It.IsAny<string?>(),
            It.IsAny<CefrLevel?>(),
            It.IsAny<PartOfSpeech?>(),
            It.IsAny<Guid?>(),
            It.IsAny<ExamType?>(),
            It.IsAny<LearningStatus?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { existingWord });

        _mockVocabRepo.Setup(r => r.GetByTermAsync("ubiquitous", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWord);
        _mockVocabRepo.Setup(r => r.GetByTermAsync("serendipity", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        // Act
        var result = await _service.AnalyzeSmartImportAsync(csvContent);

        // Assert
        result.TotalParsed.Should().Be(2);
        result.DuplicateCount.Should().Be(1);
        result.ValidCount.Should().Be(1); // 1 new valid word, 1 duplicate

        var dupItem = result.PreviewItems.First(x => x.Term == "ubiquitous");
        dupItem.IsDuplicate.Should().BeTrue();

        var newItem = result.PreviewItems.First(x => x.Term == "serendipity");
        newItem.IsDuplicate.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeSmartImportAsync_MissingMeaning_ShouldFlagAsInvalid()
    {
        // Arrange
        var txtContent = "word_without_meaning\t\t\n" +
                         "valid_word\tmeaning of valid word\n";

        _mockVocabRepo.Setup(r => r.GetByTermAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        // Act
        var result = await _service.AnalyzeSmartImportAsync(txtContent, "vocab.tsv");

        // Assert
        result.TotalParsed.Should().Be(2);
        result.InvalidCount.Should().Be(1);

        var invalidItem = result.PreviewItems.First(x => x.Term == "word_without_meaning");
        invalidItem.IsValid.Should().BeFalse();
        invalidItem.ValidationMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CommitSmartImportAsync_ValidItems_ShouldPersistViaUnitOfWork()
    {
        // Arrange
        var item = new SmartImportItemDto(
            RowIndex: 1,
            Term: "resilient",
            Level: CefrLevel.None,
            PartOfSpeech: PartOfSpeech.Adjective,
            DefinitionVi: "able to withstand or recover quickly from difficult conditions",
            DefinitionEn: null,
            PhoneticUk: null,
            PhoneticUs: null,
            Examples: null,
            Topics: null,
            ExamTags: null,
            Notes: null,
            IsDuplicate: false,
            IsValid: true,
            ValidationMessage: null,
            IsSelected: true
        );

        var request = new SmartImportCommitRequestDto(
            Items: new List<SmartImportItemDto> { item },
            ConflictResolution: ImportConflictResolution.Skip
        );

        _mockVocabRepo.Setup(r => r.GetByTermAsync("resilient", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        // Act
        var result = await _service.CommitSmartImportAsync(request);

        // Assert
        result.TotalImported.Should().Be(1);
        result.TotalErrors.Should().Be(0);
        _mockVocabRepo.Verify(r => r.AddAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitSmartImportAsync_DuplicateWithSkip_ShouldNotOverwrite()
    {
        // Arrange
        var existing = new Word { Term = "existing" };
        _mockVocabRepo.Setup(r => r.GetByTermAsync("existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var item = new SmartImportItemDto(
            RowIndex: 1,
            Term: "existing",
            Level: CefrLevel.None,
            PartOfSpeech: PartOfSpeech.Noun,
            DefinitionVi: "new meaning",
            DefinitionEn: null,
            PhoneticUk: null,
            PhoneticUs: null,
            Examples: null,
            Topics: null,
            ExamTags: null,
            Notes: null,
            IsDuplicate: true,
            IsValid: true,
            ValidationMessage: null,
            IsSelected: true
        );

        var request = new SmartImportCommitRequestDto(
            Items: new List<SmartImportItemDto> { item },
            ConflictResolution: ImportConflictResolution.Skip
        );

        // Act
        var result = await _service.CommitSmartImportAsync(request);

        // Assert
        result.TotalSkipped.Should().Be(1);
        result.TotalImported.Should().Be(0);
        _mockVocabRepo.Verify(r => r.AddAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeSmartImportAsync_JsonWithMultiPosAndMeanings_ShouldParseRichStructure()
    {
        // Arrange
        var jsonContent = """
        [
          {
            "term": "Shortlist",
            "phoneticUk": "/ˈʃɔːt.lɪst/",
            "phoneticUs": "/ˈʃɔːrt.lɪst/",
            "level": "B2",
            "primaryPartOfSpeech": "Noun",
            "allPartsOfSpeech": [
              "Noun",
              "Verb"
            ],
            "notes": null,
            "meanings": [
              {
                "partOfSpeech": "Noun",
                "definitionVi": "Danh sách sàng lọc, danh sách rút gọn",
                "definitionEn": "A list of people who have been judged the most suitable for a job from a larger group",
                "explanation": "Danh sách các ứng viên sáng giá nhất được chọn vào vòng tuyển dụng kế tiếp.",
                "examples": [
                  {
                    "sentenceEn": "His name has been included on the final shortlist for the branch manager role.",
                    "sentenceVi": "Tên của anh ấy đã được đưa vào danh sách rút gọn cuối cùng cho vai trò quản lý chi nhánh."
                  }
                ]
              },
              {
                "partOfSpeech": "Verb",
                "definitionVi": "Sàng lọc, đưa vào danh sách rút gọn",
                "definitionEn": "To put someone or something on a shortlist",
                "explanation": "Hành động chọn lọc các hồ sơ tiềm năng nhất từ nhóm lớn ứng viên.",
                "examples": [
                  {
                    "sentenceEn": "The hiring manager shortlisted three candidates for the final round.",
                    "sentenceVi": "Trưởng phòng nhân sự đã sàng lọc ba ứng viên cho vòng tuyển dụng cuối cùng."
                  }
                ]
              }
            ],
            "topics": [
              "Personnel & HR",
              "Recruitment"
            ],
            "examTags": [
              "TOEIC 450-650"
            ]
          }
        ]
        """;

        _mockVocabRepo.Setup(r => r.GetByTermAsync("Shortlist", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        // Act
        var result = await _service.AnalyzeSmartImportAsync(jsonContent, "shortlist.json");

        // Assert
        result.Should().NotBeNull();
        result.TotalParsed.Should().Be(1);
        result.ValidCount.Should().Be(1);
        result.DuplicateCount.Should().Be(0);

        var item = result.PreviewItems[0];
        item.Term.Should().Be("Shortlist");
        item.Level.Should().Be(CefrLevel.B2);
        item.AllPartsOfSpeech.Should().NotBeNull();
        item.AllPartsOfSpeech.Should().ContainInOrder(PartOfSpeech.Noun, PartOfSpeech.Verb);
        item.RichMeanings.Should().NotBeNull();
        item.RichMeanings!.Count.Should().Be(2);

        // First meaning
        item.RichMeanings[0].PartOfSpeech.Should().Be(PartOfSpeech.Noun);
        item.RichMeanings[0].DefinitionVi.Should().Be("Danh sách sàng lọc, danh sách rút gọn");
        item.RichMeanings[0].Examples.Count.Should().Be(1);
        item.RichMeanings[0].Examples[0].SentenceEn.Should().Contain("branch manager role");
        item.RichMeanings[0].Examples[0].SentenceVi.Should().Contain("quản lý chi nhánh");

        // Second meaning
        item.RichMeanings[1].PartOfSpeech.Should().Be(PartOfSpeech.Verb);
        item.RichMeanings[1].DefinitionVi.Should().Be("Sàng lọc, đưa vào danh sách rút gọn");
        item.RichMeanings[1].Examples.Count.Should().Be(1);
        item.RichMeanings[1].Examples[0].SentenceEn.Should().Contain("shortlisted three candidates");
        item.RichMeanings[1].Examples[0].SentenceVi.Should().Contain("Trưởng phòng nhân sự");
    }

    [Fact]
    public async Task CommitSmartImportAsync_JsonWithRichMeanings_ShouldPersistAllMeaningsAndSentences()
    {
        // Arrange
        Word? savedWord = null;
        _mockVocabRepo.Setup(r => r.GetByTermAsync("Shortlist", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);
        _mockVocabRepo.Setup(r => r.GetAllTopicsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Topic>());
        _mockVocabRepo.Setup(r => r.GetAllExamTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExamTag>());
        _mockVocabRepo.Setup(r => r.AddAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()))
            .Callback<Word, CancellationToken>((w, _) => savedWord = w)
            .ReturnsAsync((Word w, CancellationToken _) => w);

        var item = new SmartImportItemDto(
            RowIndex: 1,
            Term: "Shortlist",
            Level: CefrLevel.B2,
            PartOfSpeech: PartOfSpeech.Noun,
            DefinitionVi: "Danh sách sàng lọc, danh sách rút gọn",
            DefinitionEn: "A list of people...",
            PhoneticUk: "/ˈʃɔːt.lɪst/",
            PhoneticUs: "/ˈʃɔːrt.lɪst/",
            Examples: "His name has been included...",
            Topics: "Personnel & HR; Recruitment",
            ExamTags: "TOEIC 450-650",
            Notes: null,
            IsDuplicate: false,
            IsValid: true,
            ValidationMessage: null,
            IsSelected: true,
            RichMeanings: new List<WordMeaningCreateDto>
            {
                new(
                    PartOfSpeech: PartOfSpeech.Noun,
                    DefinitionVi: "Danh sách sàng lọc, danh sách rút gọn",
                    DefinitionEn: "A list of people who have been judged the most suitable for a job from a larger group",
                    Explanation: "Danh sách các ứng viên sáng giá nhất được chọn vào vòng tuyển dụng kế tiếp.",
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("His name has been included on the final shortlist for the branch manager role.", "Tên của anh ấy đã được đưa vào danh sách rút gọn cuối cùng cho vai trò quản lý chi nhánh.", null)
                    }
                ),
                new(
                    PartOfSpeech: PartOfSpeech.Verb,
                    DefinitionVi: "Sàng lọc, đưa vào danh sách rút gọn",
                    DefinitionEn: "To put someone or something on a shortlist",
                    Explanation: "Hành động chọn lọc các hồ sơ tiềm năng nhất từ nhóm lớn ứng viên.",
                    Examples: new List<ExampleSentenceCreateDto>
                    {
                        new("The hiring manager shortlisted three candidates for the final round.", "Trưởng phòng nhân sự đã sàng lọc ba ứng viên cho vòng tuyển dụng cuối cùng.", null)
                    }
                )
            },
            AllPartsOfSpeech: new List<PartOfSpeech> { PartOfSpeech.Noun, PartOfSpeech.Verb }
        );

        var request = new SmartImportCommitRequestDto(
            Items: new List<SmartImportItemDto> { item },
            ConflictResolution: ImportConflictResolution.Skip
        );

        // Act
        var result = await _service.CommitSmartImportAsync(request);

        // Assert
        result.TotalImported.Should().Be(1);
        result.TotalErrors.Should().Be(0);

        savedWord.Should().NotBeNull();
        savedWord!.Term.Should().Be("Shortlist");
        savedWord.Meanings.Count.Should().Be(2);

        // Both meanings and both sentence translations persisted
        var meaningsList = savedWord.Meanings.OrderBy(m => m.OrderIndex).ToList();
        meaningsList[0].PartOfSpeech.Should().Be(PartOfSpeech.Noun);
        meaningsList[0].Examples.Count.Should().Be(1);
        meaningsList[0].Examples.First().SentenceEn.Should().Contain("branch manager role");
        meaningsList[0].Examples.First().SentenceVi.Should().Contain("quản lý chi nhánh");

        meaningsList[1].PartOfSpeech.Should().Be(PartOfSpeech.Verb);
        meaningsList[1].Examples.Count.Should().Be(1);
        meaningsList[1].Examples.First().SentenceEn.Should().Contain("shortlisted three candidates");
        meaningsList[1].Examples.First().SentenceVi.Should().Contain("Trưởng phòng nhân sự");

        // Topics and exam tags created and linked
        savedWord.WordTopics.Count.Should().Be(2);
        savedWord.WordExamTags.Count.Should().Be(1);
        savedWord.WordExamTags.First().ExamTagId.Should().NotBeEmpty();
    }
}
