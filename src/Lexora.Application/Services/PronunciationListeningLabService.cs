using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class PronunciationListeningLabService(IVocabularyRepository vocabRepo) : IPronunciationListeningLabService
{
    private static readonly (string PairLabel, string WordA, string PhoneticA, string MeaningA, string WordB, string PhoneticB, string MeaningB, string Tip)[] CuratedMinimalPairs =
    [
        ("/iː/ vs /ɪ/", "sheep", "/ʃiːp/", "con cừu", "ship", "/ʃɪp/", "con tàu", "Kéo dài nguyên âm /iː/ (miệng mở rộng như mỉm cười), trong khi /ɪ/ phát âm ngắn và thả lỏng cơ miệng."),
        ("/iː/ vs /ɪ/", "leave", "/liːv/", "rời đi", "live", "/lɪv/", "sống", "Âm /iː/ trong 'leave' có trường độ dài gấp đôi âm /ɪ/ ngắn trong 'live'."),
        ("/æ/ vs /e/", "bad", "/bæd/", "tồi tệ", "bed", "/bed/", "cái giường", "Hạ quai hàm sâu hơn khi phát âm /æ/ (bẹt miệng), trong khi /e/ miệng mở vừa phải."),
        ("/æ/ vs /e/", "pan", "/pæn/", "cái chảo", "pen", "/pen/", "cây bút", "Khẩu hình /æ/ kéo căng khóe môi sang hai bên và hạ cằm sâu hơn /e/."),
        ("/θ/ vs /s/", "think", "/θɪŋk/", "suy nghĩ", "sink", "/sɪŋk/", "chìm/bồn rửa", "Đặt đầu lưỡi kẹp nhẹ giữa hai hàm răng cho âm /θ/, không rụt lưỡi như /s/."),
        ("/θ/ vs /s/", "thick", "/θɪk/", "dày", "sick", "/sɪk/", "ốm/bệnh", "Âm /θ/ là âm xát răng-lưỡi vô thanh, hơi đi qua kẽ giữa đầu lưỡi và răng cửa."),
        ("/p/ vs /b/", "pat", "/pæt/", "vỗ nhẹ", "bat", "/bæt/", "con dơi", "Âm /p/ là âm vô thanh bật hơi mạnh, còn /b/ là âm hữu thanh có rung thanh quản."),
        ("/l/ vs /r/", "light", "/laɪt/", "ánh sáng", "right", "/raɪt/", "đúng đắn", "Đầu lưỡi chạm nướu trên khi phát âm /l/, còn /r/ đầu lưỡi cong về sau không chạm vòm họng."),
        ("/l/ vs /r/", "lead", "/liːd/", "dẫn dắt", "read", "/riːd/", "đọc", "Giữ đầu lưỡi áp sát chân răng trên khi bắt đầu /l/, còn /r/ cần chu môi nhẹ."),
        ("/v/ vs /w/", "vest", "/vest/", "áo gi-lê", "west", "/west/", "phía tây", "Răng cửa trên chạm nhẹ môi dưới cho /v/, còn /w/ thì tròn môi chu ra trước."),
        ("/d/ vs /t/", "hard", "/hɑːd/", "cứng/chăm chỉ", "heart", "/hɑːt/", "trái tim", "Âm /d/ là âm hữu thanh có rung dây thanh, còn âm /t/ là âm vô thanh bật hơi."),
        ("/ʊ/ vs /uː/", "pull", "/pʊl/", "kéo", "pool", "/puːl/", "hồ bơi", "Âm /uː/ trong 'pool' tròn môi và ngân dài, còn /ʊ/ trong 'pull' ngắn và thả lỏng.")
    ];

    private static readonly (string Term, string PhoneticUk, string PhoneticUs, string MeaningVi, string Difference)[] CuratedAccentDifferences =
    [
        ("schedule", "/ˈʃedʒ.uːl/", "/ˈskedʒ.uːl/", "lịch trình", "UK phát âm khởi đầu bằng âm /ʃ/ ('shed-'), trong khi US phát âm âm /sk/ ('sked-')."),
        ("privacy", "/ˈprɪv.ə.si/", "/ˈpraɪ.və.si/", "sự riêng tư", "UK dùng nguyên âm ngắn /ɪ/ ở âm tiết đầu ('priv-'), US dùng nguyên âm đôi /aɪ/ ('pry-')."),
        ("advertisement", "/ədˈvɜː.tɪs.mənt/", "/ˌæd.vɚˈtaɪz.mənt/", "quảng cáo", "Trọng âm rơi vào âm tiết 2 trong UK ('ad-VER-tise-ment'), rơi vào âm tiết 3 trong US ('ad-ver-TIZE-ment')."),
        ("tomato", "/təˈmɑː.təʊ/", "/təˈmeɪ.toʊ/", "quả cà chua", "UK dùng nguyên âm /ɑː/ ('to-MAH-to'), US dùng nguyên âm /eɪ/ với tapped 't' ('to-MAY-to')."),
        ("neither", "/ˈnaɪ.ðər/", "/ˈniː.ðɚ/", "cả hai đều không", "UK thường bắt đầu bằng /naɪ-/, còn US bắt đầu bằng /niː-/ kèm âm r-uốn lưỡi cuối."),
        ("leisure", "/ˈleʒ.ər/", "/ˈliː.ʒɚ/", "thời gian rảnh", "UK phát âm /leʒ-/ (nguyên âm /e/), US phát âm /liːʒ-/ (nguyên âm /iː/)."),
        ("vitamin", "/ˈvɪt.ə.mɪn/", "/ˈvaɪ.t̬ə.mɪn/", "vitamin", "UK dùng nguyên âm ngắn /ɪ/ ('VIT-a-min'), US dùng /aɪ/ ('VAI-tuh-min') với âm 't' lướt."),
        ("herb", "/hɜːb/", "/ɝːb/", "thảo mộc", "UK phát âm phụ âm /h/ rõ ràng, trong khi US âm /h/ là âm câm ('erb')."),
        ("garage", "/ˈɡær.ɑːʒ/", "/ɡəˈrɑːʒ/", "nhà để xe", "Trọng âm rơi vào âm tiết đầu trong UK ('GAR-ahzh'), rơi vào âm tiết thứ hai trong US ('ga-RAHZH')."),
        ("water", "/ˈwɔː.tər/", "/ˈwɑː.t̬ɚ/", "nước", "UK phát âm âm /ɔː/ tròn môi không bật 'r', US dùng âm /ɑː/ mở rộng với tapped 't' và r-coloring.")
    ];

    public async Task<ListeningDictationChallengeDto?> GetNextDictationChallengeAsync(
        CefrLevel? level = null,
        CancellationToken cancellationToken = default)
    {
        var candidates = await vocabRepo.SearchAsync(
            level: level,
            take: 100,
            cancellationToken: cancellationToken);

        if (candidates.Count == 0)
        {
            candidates = await vocabRepo.SearchAsync(take: 100, cancellationToken: cancellationToken);
        }

        if (candidates.Count == 0) return null;

        var picked = candidates[Random.Shared.Next(candidates.Count)];
        var meaning = picked.Meanings.FirstOrDefault();
        var example = meaning?.Examples.FirstOrDefault();

        return new ListeningDictationChallengeDto(
            picked.Id,
            picked.Term,
            picked.PhoneticUk ?? picked.PhoneticUs,
            picked.PhoneticUs ?? picked.PhoneticUk,
            meaning?.DefinitionVi ?? picked.Term,
            meaning?.DefinitionEn,
            picked.Level.ToString(),
            example?.SentenceEn,
            example?.SentenceVi
        );
    }

    public DictationEvaluationResultDto EvaluateDictation(string userInput, string expectedTerm, string? phonetic = null)
    {
        var safeExpected = expectedTerm ?? string.Empty;
        var cleanInput = (userInput ?? string.Empty).Trim().ToLowerInvariant();
        var cleanExpected = safeExpected.Trim().ToLowerInvariant();

        if (string.Equals(cleanInput, cleanExpected, StringComparison.OrdinalIgnoreCase))
        {
            return new DictationEvaluationResultDto(
                IsExactMatch: true,
                LevenshteinDistance: 0,
                AccuracyPercent: 100.0,
                ExpectedTerm: safeExpected,
                UserInput: userInput ?? string.Empty,
                FeedbackMessage: "Chính xác tuyệt đối! Tai nghe và chính tả rất xuất sắc.",
                Phonetic: phonetic
            );
        }

        int distance = ComputeLevenshteinDistance(cleanInput, cleanExpected);
        int maxLen = Math.Max(cleanInput.Length, cleanExpected.Length);
        double accuracy = maxLen == 0 ? 0 : Math.Max(0.0, (1.0 - (double)distance / maxLen) * 100.0);

        string feedback;
        if (distance <= 2 && accuracy >= 70.0)
        {
            feedback = $"Gần đúng! Bạn chỉ nhầm {distance} ký tự. Hãy chú ý chính tả của từ '{safeExpected}'.";
        }
        else
        {
            feedback = $"Chưa chính xác. Từ phát âm là '{safeExpected}'. Hãy nghe lại phát âm và ghi nhớ nhé.";
        }

        return new DictationEvaluationResultDto(
            IsExactMatch: false,
            LevenshteinDistance: distance,
            AccuracyPercent: Math.Round(accuracy, 1),
            ExpectedTerm: safeExpected,
            UserInput: userInput ?? string.Empty,
            FeedbackMessage: feedback,
            Phonetic: phonetic
        );
    }

    public Task<IReadOnlyList<MinimalPairChallengeDto>> GetMinimalPairChallengesAsync(
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        int effectiveCount = Math.Clamp(count, 1, CuratedMinimalPairs.Length);
        var shuffled = CuratedMinimalPairs.OrderBy(_ => Random.Shared.Next()).Take(effectiveCount).ToList();

        var result = new List<MinimalPairChallengeDto>();
        foreach (var pair in shuffled)
        {
            bool pickA = Random.Shared.Next(2) == 0;
            var targetWord = pickA ? pair.WordA : pair.WordB;
            var targetPhonetic = pickA ? pair.PhoneticA : pair.PhoneticB;

            result.Add(new MinimalPairChallengeDto(
                Guid.NewGuid(),
                pair.PairLabel,
                pair.WordA,
                pair.PhoneticA,
                pair.MeaningA,
                pair.WordB,
                pair.PhoneticB,
                pair.MeaningB,
                targetWord,
                targetPhonetic,
                pair.Tip
            ));
        }

        return Task.FromResult<IReadOnlyList<MinimalPairChallengeDto>>(result);
    }

    public Task<IReadOnlyList<AccentComparisonItemDto>> GetAccentComparisonListAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        int effectiveCount = Math.Clamp(count, 1, CuratedAccentDifferences.Length);
        var items = CuratedAccentDifferences.Take(effectiveCount).Select(item =>
            new AccentComparisonItemDto(
                Guid.NewGuid(),
                item.Term,
                item.PhoneticUk,
                item.PhoneticUs,
                item.MeaningVi,
                item.Difference
            )).ToList();

        return Task.FromResult<IReadOnlyList<AccentComparisonItemDto>>(items);
    }

    public static int ComputeLevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
