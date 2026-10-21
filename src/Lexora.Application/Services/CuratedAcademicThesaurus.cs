namespace Lexora.Application.Services;

public static class CuratedAcademicThesaurus
{
    public record CuratedRelation(string RelatedTerm, string RelationType, double Weight, string Explanation);

    private static readonly Dictionary<string, List<CuratedRelation>> Relations = new(StringComparer.OrdinalIgnoreCase);

    static CuratedAcademicThesaurus()
    {
        // Synonyms bidirectional
        AddSynonymPair("accurate", "precise", 0.95, "Chính xác, chuẩn xác tuyệt đối");
        AddSynonymPair("accurate", "exact", 0.92, "Chính xác từng chi tiết");
        AddSynonymPair("abandon", "desert", 0.92, "Từ bỏ, bỏ rơi nơi chốn hoặc người");
        AddSynonymPair("abandon", "relinquish", 0.90, "Từ bỏ quyền lợi, nhượng lại");
        AddSynonymPair("abandon", "forsake", 0.88, "Bỏ rơi hoàn toàn (văn phong trang trọng)");
        AddSynonymPair("abundant", "plentiful", 0.95, "Dồi dào, phong phú về số lượng");
        AddSynonymPair("abundant", "copious", 0.90, "Nhiều, chứa lượng lớn (học thuật)");
        AddSynonymPair("achieve", "accomplish", 0.95, "Đạt được mục tiêu, hoàn thành xuất sắc");
        AddSynonymPair("achieve", "attain", 0.92, "Đạt tới trình độ, cột mốc (IELTS/Academic)");
        AddSynonymPair("acquire", "obtain", 0.95, "Đạt được, thu nhận được (kỹ năng/tài sản)");
        AddSynonymPair("acquire", "procure", 0.88, "Mua sắm, tìm kiếm được nguồn hàng");
        AddSynonymPair("adapt", "adjust", 0.94, "Thích nghi, điều chỉnh cho phù hợp");
        AddSynonymPair("adequate", "sufficient", 0.95, "Đầy đủ, đáp ứng trọn vẹn yêu cầu");
        AddSynonymPair("advocate", "champion", 0.90, "Ủng hộ mạnh mẽ, bảo vệ quan điểm");
        AddSynonymPair("ambiguous", "vague", 0.94, "Mơ hồ, không rõ ràng");
        AddSynonymPair("ambiguous", "equivocal", 0.90, "Đa nghĩa, lập lờ (học thuật)");
        AddSynonymPair("analyze", "examine", 0.94, "Phân tích, xem xét kỹ lưỡng");
        AddSynonymPair("analyze", "scrutinize", 0.92, "Soi xét, phân tích tỉ mỉ");
        AddSynonymPair("benefit", "advantage", 0.94, "Lợi ích, thế mạnh vượt trội");
        AddSynonymPair("capable", "competent", 0.95, "Có năng lực, thuần thục nghiệp vụ");
        AddSynonymPair("complex", "complicated", 0.95, "Phức tạp, gồm nhiều thành phần liên kết");
        AddSynonymPair("complex", "intricate", 0.92, "Tinh vi, rắc rối chi tiết");
        AddSynonymPair("comprehend", "understand", 0.95, "Thấu hiểu, lĩnh hội toàn diện");
        AddSynonymPair("comprehend", "grasp", 0.92, "Nắm bắt được ý niệm then chốt");
        AddSynonymPair("crucial", "vital", 0.96, "Vô cùng trọng yếu, mang tính sống còn");
        AddSynonymPair("crucial", "essential", 0.94, "Cốt lõi, không thể thiếu");
        AddSynonymPair("decline", "decrease", 0.95, "Sụt giảm, hạ xuống mức thấp hơn");
        AddSynonymPair("diverse", "varied", 0.95, "Đa dạng, phong phú nhiều chủng loại");
        AddSynonymPair("diverse", "heterogeneous", 0.88, "Không đồng nhất, đa sắc thái");
        AddSynonymPair("eliminate", "remove", 0.95, "Loại bỏ, triệt tiêu hoàn toàn");
        AddSynonymPair("eliminate", "eradicate", 0.92, "Xóa bỏ tận gốc rễ");
        AddSynonymPair("enhance", "improve", 0.95, "Nâng cao chất lượng, cải thiện");
        AddSynonymPair("enhance", "augment", 0.90, "Gia tăng giá trị, làm mạnh thêm");
        AddSynonymPair("evident", "obvious", 0.95, "Rõ ràng, hiển nhiên");
        AddSynonymPair("evident", "apparent", 0.92, "Biểu hiện rõ, dễ nhận thấy");
        AddSynonymPair("expand", "extend", 0.93, "Mở rộng quy mô, trải rộng phạm vi");
        AddSynonymPair("flexible", "adaptable", 0.95, "Linh hoạt, thích ứng nhanh");
        AddSynonymPair("fundamental", "essential", 0.94, "Căn bản, mang tính nền tảng");
        AddSynonymPair("generate", "produce", 0.95, "Tạo ra, sản sinh ra thành phẩm/năng lượng");
        AddSynonymPair("highlight", "emphasize", 0.96, "Nhấn mạnh, làm nổi bật trọng tâm");
        AddSynonymPair("illustrate", "demonstrate", 0.95, "Minh họa, chứng minh cụ thể");
        AddSynonymPair("impact", "influence", 0.95, "Ảnh hưởng, tác động sâu rộng");
        AddSynonymPair("initial", "primary", 0.92, "Ban đầu, nguyên khởi");
        AddSynonymPair("isolate", "separate", 0.94, "Cách ly, phân tách độc lập");
        AddSynonymPair("maintain", "preserve", 0.94, "Duy trì, giữ gìn hiện trạng nguyên vẹn");
        AddSynonymPair("maintain", "sustain", 0.93, "Chống đỡ, duy trì liên tục lâu dài");
        AddSynonymPair("negotiate", "bargain", 0.92, "Đàm phán, thương lượng thỏa thuận");
        AddSynonymPair("objective", "impartial", 0.95, "Khách quan, không thiên vị");
        AddSynonymPair("participate", "partake", 0.90, "Tham gia, cùng góp mặt");
        AddSynonymPair("persuade", "convince", 0.96, "Thuyết phục làm thay đổi nhận thức");
        AddSynonymPair("reluctant", "hesitant", 0.95, "Lưỡng lự, e ngại, không sẵn lòng");
        AddSynonymPair("restrict", "limit", 0.95, "Hạn chế, giới hạn phạm vi");
        AddSynonymPair("scarce", "rare", 0.94, "Khan hiếm, ít thấy");
        AddSynonymPair("significant", "substantial", 0.95, "Đáng kể, có tầm ảnh hưởng lớn");
        AddSynonymPair("subsequent", "following", 0.94, "Tiếp theo sau, xảy ra kế tiếp");
        AddSynonymPair("temporary", "transient", 0.93, "Tạm thời, mang tính ngắn hạn");
        AddSynonymPair("vague", "indistinct", 0.92, "Mơ hồ, mờ nhạt không rõ nét");
        AddSynonymPair("vital", "indispensable", 0.95, "Tối quan trọng, không thể thiếu được");

        // Antonyms bidirectional
        AddAntonymPair("abundant", "scarce", 0.95, "Dồi dào vs. Khan hiếm");
        AddAntonymPair("abundant", "meager", 0.90, "Phong phú vs. Ít ỏi, nghèo nàn");
        AddAntonymPair("accurate", "inaccurate", 0.95, "Chính xác vs. Không chính xác");
        AddAntonymPair("accurate", "erroneous", 0.92, "Chính xác vs. Sai lầm, lệch chuẩn");
        AddAntonymPair("achieve", "fail", 0.95, "Đạt thành tích vs. Thất bại");
        AddAntonymPair("acquire", "lose", 0.94, "Thu được vs. Đánh mất");
        AddAntonymPair("adequate", "inadequate", 0.96, "Đầy đủ vs. Thiếu thốn, không đủ");
        AddAntonymPair("advocate", "oppose", 0.95, "Ủng hộ vs. Phản đối gay gắt");
        AddAntonymPair("ambiguous", "clear", 0.95, "Mơ hồ vs. Rõ ràng, minh bạch");
        AddAntonymPair("complex", "simple", 0.96, "Phức tạp vs. Đơn giản");
        AddAntonymPair("comprehend", "misunderstand", 0.95, "Thấu hiểu vs. Hiểu sai, hiểu lầm");
        AddAntonymPair("crucial", "trivial", 0.95, "Trọng yếu vs. Tầm thường, vụn vặt");
        AddAntonymPair("decline", "increase", 0.96, "Sụt giảm vs. Gia tăng");
        AddAntonymPair("decline", "grow", 0.92, "Thoái lui vs. Tăng trưởng");
        AddAntonymPair("diverse", "uniform", 0.95, "Đa dạng vs. Đồng nhất, đơn điệu");
        AddAntonymPair("eliminate", "introduce", 0.92, "Xóa bỏ vs. Đưa vào áp dụng");
        AddAntonymPair("enhance", "diminish", 0.94, "Nâng cao vs. Làm suy giảm");
        AddAntonymPair("evident", "obscure", 0.94, "Hiển nhiên vs. Mờ tối, khó hiểu");
        AddAntonymPair("expand", "shrink", 0.95, "Giãn nở, mở rộng vs. Co lại, thu hẹp");
        AddAntonymPair("expand", "contract", 0.94, "Mở rộng vs. Thu hẹp quy mô");
        AddAntonymPair("flexible", "rigid", 0.96, "Linh hoạt vs. Cứng nhắc, bất di bất dịch");
        AddAntonymPair("fundamental", "superficial", 0.94, "Cốt lõi, căn bản vs. Nông cạn, bề ngoài");
        AddAntonymPair("generate", "destroy", 0.94, "Kiến tạo vs. Hủy diệt, tiêu biến");
        AddAntonymPair("highlight", "downplay", 0.94, "Nhấn mạnh vs. Xem nhẹ, giảm nhẹ");
        AddAntonymPair("initial", "final", 0.96, "Khởi đầu vs. Kết thúc sau cùng");
        AddAntonymPair("isolate", "integrate", 0.95, "Cách ly vs. Hòa nhập, tích hợp");
        AddAntonymPair("maintain", "abandon", 0.95, "Duy trì vs. Từ bỏ");
        AddAntonymPair("objective", "subjective", 0.96, "Khách quan vs. Chủ quan");
        AddAntonymPair("participate", "abstain", 0.94, "Tham gia vs. Kiêng cử, rút lui");
        AddAntonymPair("persuade", "dissuade", 0.95, "Thuyết phục làm vs. Can ngăn");
        AddAntonymPair("precise", "vague", 0.95, "Chuẩn xác vs. Mơ hồ");
        AddAntonymPair("reluctant", "willing", 0.96, "Miễn cưỡng vs. Sẵn lòng");
        AddAntonymPair("reluctant", "eager", 0.94, "Do dự vs. Háo hức");
        AddAntonymPair("restrict", "release", 0.92, "Hạn chế vs. Phóng thích, mở rộng");
        AddAntonymPair("scarce", "abundant", 0.95, "Khan hiếm vs. Dồi dào");
        AddAntonymPair("significant", "insignificant", 0.96, "Quan trọng vs. Không đáng kể");
        AddAntonymPair("subsequent", "prior", 0.95, "Kế tiếp vs. Trước đó");
        AddAntonymPair("temporary", "permanent", 0.96, "Tạm thời vs. Vĩnh viễn, trường cửu");
        AddAntonymPair("vital", "dispensable", 0.94, "Sống còn vs. Có thể bỏ qua được");

        // Word families
        AddWordFamilyGroup(
            new[] { "abandon", "abandonment", "abandoned" },
            "Gốc từ abandon (Từ bỏ / Sự từ bỏ / Bị bỏ rơi)");
        AddWordFamilyGroup(
            new[] { "achieve", "achievement", "achievable" },
            "Gốc từ achieve (Đạt được / Thành tựu / Khả thi)");
        AddWordFamilyGroup(
            new[] { "accurate", "accuracy", "accurately", "inaccurate" },
            "Gốc từ accurate (Chính xác / Độ chính xác / Chuẩn xác / Thiếu chính xác)");
        AddWordFamilyGroup(
            new[] { "analyze", "analysis", "analyst", "analytical", "analytically" },
            "Gốc từ analyze (Phân tích / Bản phân tích / Chuyên viên / Thuộc phân tích)");
        AddWordFamilyGroup(
            new[] { "benefit", "beneficial", "beneficiary", "beneficially" },
            "Gốc từ benefit (Lợi ích / Có lợi / Người thụ hưởng)");
        AddWordFamilyGroup(
            new[] { "comprehend", "comprehension", "comprehensive", "comprehensibly" },
            "Gốc từ comprehend (Hiểu biết / Nhận thức / Toàn diện)");
        AddWordFamilyGroup(
            new[] { "decide", "decision", "decisive", "decisively" },
            "Gốc từ decide (Quyết định / Sự quyết định / Dứt khoát)");
        AddWordFamilyGroup(
            new[] { "diverse", "diversity", "diversify", "diversification" },
            "Gốc từ diverse (Đa dạng / Sự đa dạng / Đa dạng hóa)");
        AddWordFamilyGroup(
            new[] { "expand", "expansion", "expansive", "expandable" },
            "Gốc từ expand (Mở rộng / Sự mở rộng / Rộng lớn)");
        AddWordFamilyGroup(
            new[] { "maintain", "maintenance", "maintainable" },
            "Gốc từ maintain (Duy trì / Sự bảo trì / Có thể duy trì)");
        AddWordFamilyGroup(
            new[] { "rely", "reliance", "reliable", "reliably", "unreliable" },
            "Gốc từ rely (Dựa vào / Sự tin cậy / Đáng tin cậy / Không đáng tin)");
        AddWordFamilyGroup(
            new[] { "succeed", "success", "successful", "successfully" },
            "Gốc từ succeed (Kế vị, thành công / Sự thành công / Thành công)");
    }

    private static void AddSynonymPair(string a, string b, double weight, string explanation)
    {
        AddEntry(a, new CuratedRelation(b, "Synonym", weight, explanation));
        AddEntry(b, new CuratedRelation(a, "Synonym", weight, explanation));
    }

    private static void AddAntonymPair(string a, string b, double weight, string explanation)
    {
        AddEntry(a, new CuratedRelation(b, "Antonym", weight, explanation));
        AddEntry(b, new CuratedRelation(a, "Antonym", weight, explanation));
    }

    private static void AddWordFamilyGroup(string[] words, string explanation)
    {
        for (int i = 0; i < words.Length; i++)
        {
            for (int j = 0; j < words.Length; j++)
            {
                if (i == j) continue;
                AddEntry(words[i], new CuratedRelation(words[j], "WordFamily", 0.90, explanation));
            }
        }
    }

    private static void AddEntry(string term, CuratedRelation relation)
    {
        var key = term.Trim().ToLowerInvariant();
        if (!Relations.TryGetValue(key, out var list))
        {
            list = new List<CuratedRelation>();
            Relations[key] = list;
        }

        if (!list.Any(r => string.Equals(r.RelatedTerm, relation.RelatedTerm, StringComparison.OrdinalIgnoreCase) && r.RelationType == relation.RelationType))
        {
            list.Add(relation);
        }
    }

    public static IReadOnlyList<CuratedRelation> GetRelationsForTerm(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return Array.Empty<CuratedRelation>();
        var key = term.Trim().ToLowerInvariant();
        return Relations.TryGetValue(key, out var list) ? list : Array.Empty<CuratedRelation>();
    }
}
