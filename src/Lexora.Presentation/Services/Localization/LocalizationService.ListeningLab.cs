namespace Lexora.Presentation.Services;

public partial class LocalizationService
{
    private static void RegisterListeningLab(Dictionary<string, string> vi, Dictionary<string, string> en)
    {
        // ==========================================
        // ListeningLab — vi-VN (Tiếng Việt)
        // ==========================================
        // Pronunciation & Listening Lab
        vi["ListeningLab_Title"] = "Phòng Luyện Phát âm & Nghe";
        vi["ListeningLab_Subtitle"] = "Luyện tai phân biệt âm chuẩn, chép chính tả âm thanh và làm chủ ngữ điệu Anh - Anh / Anh - Mỹ";
        vi["ListeningLab_TabDictation"] = "Chép chính tả";
        vi["ListeningLab_TabMinimalPairs"] = "Phân biệt cặp âm";
        vi["ListeningLab_TabAccentCompare"] = "So sánh UK - US";
        vi["ListeningLab_AccentCompareDesc"] = "So sánh các từ có phát âm khác biệt rõ rệt giữa tiếng Anh - Anh (UK) và Anh - Mỹ (US):";
        vi["ListeningLab_ListenPrompt"] = "Nghe phát âm từ vựng";
        vi["ListeningLab_TypeHeardWord"] = "Nhập từ bạn nghe được...";
        vi["ListeningLab_CheckAnswer"] = "Kiểm tra";
        vi["ListeningLab_NextWord"] = "Từ tiếp theo";
        vi["ListeningLab_NextPair"] = "Cặp âm tiếp theo";
        vi["ListeningLab_WhichWordHeard"] = "Bạn vừa nghe thấy từ nào?";
        vi["ListeningLab_PlayUk"] = "Nghe giọng Anh (UK)";
        vi["ListeningLab_PlayUs"] = "Nghe giọng Mỹ (US)";
        vi["ListeningLab_ArticulatoryTip"] = "Mẹo phát âm & Khẩu hình";
        vi["ListeningLab_Speed"] = "Tốc độ";
        vi["ListeningLab_Accuracy"] = "Độ chính xác";

        // ==========================================
        // ListeningLab — en-US (English)
        // ==========================================
        // Pronunciation & Listening Lab
        en["ListeningLab_Title"] = "Pronunciation & Listening Lab";
        en["ListeningLab_Subtitle"] = "Master phoneme discrimination, audio dictation, and British vs American accent nuances";
        en["ListeningLab_TabDictation"] = "Audio Dictation";
        en["ListeningLab_TabMinimalPairs"] = "Minimal Pairs";
        en["ListeningLab_TabAccentCompare"] = "UK vs US Accent Lab";
        en["ListeningLab_AccentCompareDesc"] = "Compare words with distinctive British (UK) vs American (US) pronunciations:";
        en["ListeningLab_ListenPrompt"] = "Listen to pronunciation";
        en["ListeningLab_TypeHeardWord"] = "Type the word you hear...";
        en["ListeningLab_CheckAnswer"] = "Check";
        en["ListeningLab_NextWord"] = "Next Word";
        en["ListeningLab_NextPair"] = "Next Pair";
        en["ListeningLab_WhichWordHeard"] = "Which word did you hear?";
        en["ListeningLab_PlayUk"] = "Listen British (UK)";
        en["ListeningLab_PlayUs"] = "Listen American (US)";
        en["ListeningLab_ArticulatoryTip"] = "Articulation & Mouth Shape Guide";
        en["ListeningLab_Speed"] = "Speed";
        en["ListeningLab_Accuracy"] = "Accuracy";
    }
}
