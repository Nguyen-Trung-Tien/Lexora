namespace Lexora.Domain.Enums;

/// <summary>
/// Categorization for memory tips and exam strategies.
/// </summary>
public enum TipCategory
{
    Vocabulary = 0,
    Grammar = 1,
    Tenses = 2,
    Expressions = 3,
    ExamStrategy = 4,
    General = 5
}

/// <summary>
/// Mnemonic and cognitive retention techniques.
/// </summary>
public enum MnemonicTechnique
{
    Acrostic = 0,        // Câu thần chú / Ghép chữ cái đầu (ví dụ: OSACOMP, FANBOYS)
    RhymeAndRhythm = 1,  // Thơ, vè, vần điệu
    VisualImagery = 2,   // Hình ảnh liên tưởng / Visual Hook
    SoundAlike = 3,      // Cầu nối âm thanh / Âm thanh tương tự
    Storytelling = 4,    // Câu chuyện / Ngữ cảnh liên tưởng
    Etymology = 5,       // Gốc từ / Tiền tố & Hậu tố
    EliminationRule = 6, // Dấu hiệu nhận biết & Quy tắc loại trừ đề thi
    Other = 7            // Khác
}
