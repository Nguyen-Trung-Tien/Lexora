using Lexora.Domain.Entities.MemoryTips;
using Lexora.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.Data;

public partial class SqliteDatabaseInitializer
{
    private async Task SeedMemoryTipsStarterDataAsync(CancellationToken ct)
    {
        var tips = new List<MemoryTip>
        {
            new()
            {
                Title = "Thứ tự tính từ trước danh từ (OpSASCOMP)",
                MnemonicHook = "Ông Sáu Ăn Súp Cua Ông Mập Phì (Op - S - A - S - C - O - M - P)",
                Explanation = "Quy tắc trật tự tính từ: Opinion (ý kiến) -> Size (kích cỡ) -> Age (độ tuổi) -> Shape (hình dáng) -> Color (màu sắc) -> Origin (nguồn gốc) -> Material (chất liệu) -> Purpose (mục đích).",
                Example = "A lovely small old round black Vietnamese wooden dining table.",
                CommonTrap = "Đề thi TOEIC/VSTEP thường đổi chỗ Size và Color (ví dụ 'a black small car' là SAI, phải là 'a small black car').",
                Category = TipCategory.Grammar,
                Technique = MnemonicTechnique.Acrostic,
                ExamTarget = ExamTarget.Toeic,
                IsPinned = true,
                Tags = "Adjectives, Grammar, Word Order, TOEIC, VSTEP"
            },
            new()
            {
                Title = "7 Liên từ kết hợp kinh điển (FANBOYS)",
                MnemonicHook = "FANBOYS: For - And - Nor - But - Or - Yet - So",
                Explanation = "Các liên từ đẳng lập dùng nối 2 mệnh đề độc lập. Khi nối 2 mệnh đề hoàn chỉnh, trước FANBOYS bắt buộc phải có dấu phẩy (,).",
                Example = "I wanted to score 850+ in TOEIC, so I practiced daily with Lexora.",
                CommonTrap = "Bẫy lỗi Comma Splice: Dùng dấu phẩy nối 2 mệnh đề độc lập mà không có liên từ FANBOYS, hoặc dùng 'Because' đi kèm 'So'.",
                Category = TipCategory.Grammar,
                Technique = MnemonicTechnique.Acrostic,
                ExamTarget = ExamTarget.Toeic,
                IsPinned = true,
                Tags = "Conjunctions, Clauses, Grammar, FANBOYS"
            },
            new()
            {
                Title = "Phân biệt SINCE vs FOR (Hiện tại Hoàn thành)",
                MnemonicHook = "SINCE là mốc (Point in time) — FOR là khoảng (Period of time)",
                Explanation = "'Since' đi kèm mốc thời gian cụ thể (since 2020, since yesterday, since 8 AM). 'For' đi với khoảng thời gian kéo dài (for 3 years, for 2 days, for a long time).",
                Example = "She has been working here since Monday / for two weeks.",
                CommonTrap = "Bẫy 'Since + 3 years' là sai hoàn toàn! Lưu ý: Nếu có 'the last/past + số năm' thì dùng 'For the last 3 years'.",
                Category = TipCategory.Tenses,
                Technique = MnemonicTechnique.EliminationRule,
                ExamTarget = ExamTarget.Toeic,
                IsPinned = false,
                Tags = "Tenses, Present Perfect, Since, For"
            },
            new()
            {
                Title = "Phân biệt ANOTHER, OTHER, OTHERS, THE OTHER",
                MnemonicHook = "AN-OTHER: 'An' là 1 (số ít) | OTHERS: có 'S' thay thế số nhiều | THE OTHER: Đối tượng còn lại đã xác định",
                Explanation = "Another + N số ít (một cái khác). Other + N số nhiều / không đếm được. Others đóng vai trò đại từ độc lập (những người/vật khác). The other là người/vật còn lại trong nhóm 2 đối tượng.",
                Example = "I'd like another cup of tea. Some students like reading, while others prefer listening.",
                CommonTrap = "Không bao giờ có 'Another + Danh từ số nhiều'. Không bao giờ viết 'Others + Danh từ' (phải là Other + Noun).",
                Category = TipCategory.Vocabulary,
                Technique = MnemonicTechnique.EliminationRule,
                ExamTarget = ExamTarget.Ielts,
                IsPinned = false,
                Tags = "Determiners, Vocabulary, Pronouns, IELTS"
            },
            new()
            {
                Title = "Chiến thuật TOEIC Listening Part 1 & 2: Bẫy âm tương tự",
                MnemonicHook = "Nghe giống từ khóa trong câu hỏi -> 90% LÀ BẪY (Similar Sound Distractor)!",
                Explanation = "Trong Part 1 & 2 TOEIC, các đáp án chứa từ phát âm tương tự hoặc lặp lại y hệt từ then chốt của câu hỏi thường là bẫy đánh lạc hướng người nghe.",
                Example = "Câu hỏi có 'coffee', đáp án bẫy xuất hiện 'copy'; câu hỏi có 'department', đáp án bẫy có 'departure'.",
                CommonTrap = "Thí sinh có thói quen chọn đáp án có từ nghe quen tai nhất trong câu hỏi, đây là bẫy kinh điển của đề ETS.",
                Category = TipCategory.ExamStrategy,
                Technique = MnemonicTechnique.EliminationRule,
                ExamTarget = ExamTarget.Toeic,
                IsPinned = true,
                Tags = "TOEIC, Listening, Strategy, Distractor Trap"
            },
            new()
            {
                Title = "Động từ theo sau bởi V-ing (Tránh né, Đề xuất, Trì hoãn)",
                MnemonicHook = "Avoid, Postpone, Suggest, Consider, Practice, Deny -> Luôn luôn + V-ING!",
                Explanation = "Các động từ chỉ sự trì hoãn, tránh né, đề xuất hoặc thói quen luôn đi với Gerund (V-ing) thay vì To-V.",
                Example = "He avoided answering the direct question during the interview.",
                CommonTrap = "Đề thi hay gài 'suggest to do something' (SAI). Phải là 'suggest doing something' hoặc 'suggest that S (should) do something'.",
                Category = TipCategory.Grammar,
                Technique = MnemonicTechnique.RhymeAndRhythm,
                ExamTarget = ExamTarget.Vstep,
                IsPinned = false,
                Tags = "Gerund, Verbs, Grammar, VSTEP"
            },
            new()
            {
                Title = "Mẹo nhớ tiền tố phủ định (UN-, IN-, IM-, IL-, IR-)",
                MnemonicHook = "IM đi với M/P (Impatient) — IL đi với L (Illegal) — IR đi với R (Irresponsible)",
                Explanation = "Quy tắc đồng hóa ngữ âm: tiền tố In- biến thể thành Im- khi đứng trước m/p, Il- trước l, Ir- trước r. Còn Un- là tiền tố phổ thông nhất.",
                Example = "Possible -> Impossible; Logical -> Illogical; Relevant -> Irrelevant.",
                CommonTrap = "Từ 'inflammable' không phải là không cháy, mà nghĩa là 'cực kỳ dễ cháy / bắt lửa'!",
                Category = TipCategory.Vocabulary,
                Technique = MnemonicTechnique.Etymology,
                ExamTarget = ExamTarget.Ielts,
                IsPinned = false,
                Tags = "Prefix, Etymology, Vocabulary, IELTS"
            }
        };

        await _context.MemoryTips.AddRangeAsync(tips, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Successfully seeded {Count} starter memory tips and exam mnemonics.", tips.Count);
    }
}
