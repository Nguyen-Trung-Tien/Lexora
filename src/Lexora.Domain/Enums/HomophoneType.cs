namespace Lexora.Domain.Enums;

public enum HomophoneType
{
    /// <summary>
    /// Đồng âm hoàn toàn (Cùng cách phát âm IPA, khác chính tả và nghĩa. Ví dụ: bare/bear, knight/night, flour/flower, sea/see)
    /// </summary>
    ExactHomophone = 0,

    /// <summary>
    /// Cặp âm suýt giống nhau / Dễ gây nhầm lẫn phát âm (Ví dụ: desert/dessert, accept/except, loose/lose, advice/advise)
    /// </summary>
    NearHomophone = 1,

    /// <summary>
    /// Cặp từ tối thiểu phân biệt nguyên âm/phụ âm (Minimal pairs. Ví dụ: ship/sheep, bad/bed, think/sink, light/right)
    /// </summary>
    MinimalPair = 2
}
