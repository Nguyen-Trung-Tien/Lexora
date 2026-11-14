using Lexora.Application.Common;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services.Phrases;

public class ConversationPhraseService : IConversationPhraseService
{
    private readonly IConversationPhraseRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ConversationPhraseService(
        IConversationPhraseRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ConversationPhraseDto>> GetPhrasesAsync(
        ConversationPhraseFilterDto? filter = null,
        CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetFilteredAsync(
            filter?.Category,
            filter?.CefrLevel,
            filter?.OnlyFavorites,
            filter?.SearchQuery,
            cancellationToken);

        return entities.Select(e => e.ToDto()).ToList();
    }

    public async Task<ConversationPhraseDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity?.ToDto();
    }

    public async Task<ConversationPhraseDto> CreateAsync(
        CreateConversationPhraseDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto.PromptSentence, dto.PromptTranslation, dto.SampleResponse, dto.ResponseTranslation, dto.Category);

        var phrase = new ConversationPhrase
        {
            Id = Guid.NewGuid(),
            Category = dto.Category.Trim(),
            ContextTag = (dto.ContextTag ?? string.Empty).Trim(),
            PromptSentence = dto.PromptSentence.Trim(),
            PromptTranslation = dto.PromptTranslation.Trim(),
            SampleResponse = dto.SampleResponse.Trim(),
            ResponseTranslation = dto.ResponseTranslation.Trim(),
            AdditionalSuggestions = (dto.AdditionalSuggestions ?? string.Empty).Trim(),
            CefrLevel = dto.CefrLevel,
            IsFavorite = dto.IsFavorite,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        phrase.UpdateNormalizedFields();

        await _repository.AddAsync(phrase, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return phrase.ToDto();
    }

    public async Task<ConversationPhraseDto> UpdateAsync(
        UpdateConversationPhraseDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto.PromptSentence, dto.PromptTranslation, dto.SampleResponse, dto.ResponseTranslation, dto.Category);

        var phrase = await _repository.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation phrase with ID '{dto.Id}' was not found.");

        phrase.Category = dto.Category.Trim();
        phrase.ContextTag = (dto.ContextTag ?? string.Empty).Trim();
        phrase.PromptSentence = dto.PromptSentence.Trim();
        phrase.PromptTranslation = dto.PromptTranslation.Trim();
        phrase.SampleResponse = dto.SampleResponse.Trim();
        phrase.ResponseTranslation = dto.ResponseTranslation.Trim();
        phrase.AdditionalSuggestions = (dto.AdditionalSuggestions ?? string.Empty).Trim();
        phrase.CefrLevel = dto.CefrLevel;
        phrase.IsFavorite = dto.IsFavorite;
        phrase.UpdatedAt = DateTime.UtcNow;

        phrase.UpdateNormalizedFields();

        await _repository.UpdateAsync(phrase, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return phrase.ToDto();
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var phrase = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation phrase with ID '{id}' was not found.");

        await _repository.DeleteAsync(phrase, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ToggleFavoriteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var phrase = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation phrase with ID '{id}' was not found.");

        phrase.IsFavorite = !phrase.IsFavorite;
        phrase.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(phrase, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return phrase.IsFavorite;
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetCategoriesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<PhraseSmartSuggestionDto>> GetQuickTemplatesAsync(
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var list = new List<PhraseSmartSuggestionDto>
        {
            new()
            {
                Title = "Trang trọng (Formal)",
                Description = "Cách diễn đạt lịch sự dùng trong thư từ, gặp khách hàng hoặc đàm phán",
                TextToAppend = "• Formal alternative: \"It is a great pleasure to make your acquaintance. May I inquire about... ?\"\n(Cách nói trang trọng, phù hợp công sở và đối tác)",
                CategoryTag = "Formal"
            },
            new()
            {
                Title = "Thân mật (Casual / Native)",
                Description = "Cách nói tự nhiên, gần gũi của người bản xứ giữa bạn bè, đồng nghiệp thân",
                TextToAppend = "• Casual / Native phrasing: \"What's up? Long time no see! How've things been with you?\"\n(Cách nói thân mật hàng ngày, tự nhiên)",
                CategoryTag = "Casual"
            },
            new()
            {
                Title = "Từ vựng thay thế (Synonyms & Nuances)",
                Description = "Các từ vựng nâng cấp hoặc từ đồng nghĩa giúp tránh lặp từ",
                TextToAppend = "• Vocabulary expansion:\n  - Thay vì chỉ nói \"Good\": dùng \"Remarkable\", \"Fascinating\", \"Pretty solid\".\n  - Thay vì \"I understand\": dùng \"I see your point\", \"That makes total sense\".",
                CategoryTag = "Vocabulary"
            },
            new()
            {
                Title = "Câu hỏi mở rộng (Follow-up Question)",
                Description = "Cách duy trì cuộc đối thoại liền mạch, thể hiện sự quan tâm và kết nối",
                TextToAppend = "• Follow-up question to keep the conversation flowing:\n  \"By the way, how did the presentation turn out? Would love to hear your takeaways!\"",
                CategoryTag = "FollowUp"
            },
            new()
            {
                Title = "Lưu ý ngữ điệu & Ngữ cảnh (Intonation & Cultural Note)",
                Description = "Gợi ý nhấn trọng âm câu, ngữ điệu thân thiện và điểm lưu ý văn hóa giao tiếp",
                TextToAppend = "• Intonation tip: Lên giọng nhẹ ở cuối câu hỏi để tạo cảm giác cởi mở, thân thiện. Nhấn mạnh vào động từ chỉ cảm xúc (really, absolutely, delighted).",
                CategoryTag = "Cultural"
            }
        };

        return Task.FromResult<IReadOnlyList<PhraseSmartSuggestionDto>>(list);
    }

    public Task<string> GenerateEnhancedSuggestionsAsync(
        string promptSentence,
        string sampleResponse,
        string category,
        CancellationToken cancellationToken = default)
    {
        var categoryName = string.IsNullOrWhiteSpace(category) ? "Giao tiếp" : category.Trim();
        var template = $"""
            📌 Ngữ cảnh áp dụng: {categoryName}
            • Formal variation: Diễn đạt trang trọng hơn trong môi trường học thuật hoặc công sở.
            • Casual variation: Diễn đạt tự nhiên, rút gọn khi trò chuyện cùng bạn bè bè hay đồng nghiệp thân quen.
            • Vocabulary tip: Chú ý cách dùng liên từ và giới từ đi kèm để phản hồi trôi chảy, phản xạ tự nhiên.
            • Follow-up suggestion: Đặt thêm một câu hỏi mở để tiếp tục duy trì hội thoại.
            """.Trim();

        return Task.FromResult(template);
    }

    private static void ValidateDto(
        string prompt,
        string promptTrans,
        string response,
        string responseTrans,
        string category)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt sentence cannot be empty.", nameof(prompt));

        if (string.IsNullOrWhiteSpace(promptTrans))
            throw new ArgumentException("Prompt translation cannot be empty.", nameof(promptTrans));

        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentException("Sample response cannot be empty.", nameof(response));

        if (string.IsNullOrWhiteSpace(responseTrans))
            throw new ArgumentException("Response translation cannot be empty.", nameof(responseTrans));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty.", nameof(category));
    }
}
