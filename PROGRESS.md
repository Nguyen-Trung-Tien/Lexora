# 📊 Lexora — Tiến Độ Phát Triển Dự Án (Project Progress Tracker)

> Cập nhật lần cuối: **16/09/2026** | Phiên bản hiện tại: **0.4.0-alpha (Senior Architecture & Quality Enhancement)**  
> Nền tảng: **.NET 10** • **WinUI 3 (Windows App SDK)** • **SQLite / EF Core 10** • **Clean Architecture**

---

## 🚀 Tổng Quan Tiến Độ (Dashboard)

| Giai Đoạn | Tên Module / Phase | Trạng Thái | Tiến Độ (%) | Kiểm Thử |
|---|---|:---:|:---:|:---:|
| **Phase 1** | **Foundation & Clean Architecture** | ✅ Hoàn thành | 100% | Đã verify |
| **Phase 2** | **Vocabulary Management (CRUD, Audio, Import/Export)** | ✅ Hoàn thành | 100% | 11/11 tests pass |
| **Phase 3** | **Learning & Spaced Repetition (SM-2 & Ebbinghaus)** | ✅ Hoàn thành | 100% | 13/13 tests pass |
| **Phase 4** | **Vocabulary Test & Practice (Quiz, Dictation, Fill-in)** | ✅ Hoàn thành | 100% | 8/8 tests pass |
| **Phase 5** | **Grammar & Sentence Structure** | ✅ Hoàn thành | 100% | 6/6 tests pass |
| **Phase 6** | **Dashboard, Analytics & Retention Charts** | ✅ Hoàn thành | 100% | 4/4 tests pass |
| **Phase 7** | **UI/UX Polish, Shortcuts & Accessibility** | ✅ Hoàn thành | 100% | Dynamic i18n & Intervals |
| **Phase 8** | **Backup, Packaging & Release** | ✅ Hoàn thành | 100% | Rollback & QuickCheck |
| **Phase 9** | **Senior Architecture, Security & Smart Optimization** | ✅ Hoàn thành | 100% | Batch Load, CWE-1236, PRAGMAs |
| **Phase 10** | **Intelligence, Workload Forecast & Query Optimization** | ✅ Hoàn thành | 100% | 96/96 tests pass |
| **Phase 11** | **Real-Time Dynamic i18n & Fluid Motion Transitions** | ✅ Hoàn thành | 100% | 96/96 tests pass |

**Tổng tiến độ toàn diện toàn dự án: 100% (Hoàn thành 11/11 Phases)**  
**Trạng thái Build & Test**: 96/96 Unit & Integration Tests **PASS** (70 Unit + 26 Integration, 0 errors, 0 warnings).
- **Trí thông minh học tập**: Smart Learning Advisor (`IStudyAdvisorService`), 7-Day Spaced Repetition Workload Forecast (`WorkloadForecastDto`), Typo-Tolerant & Fuzzy Search Engine (`IFuzzySearchService` với Damerau-Levenshtein).
- **Tối ưu hóa Database**: Zero-allocation `CountDueCardsAsync` và `GetDueForecastCountsAsync` trong `ReviewRepository` & `ReviewService`.
- **Giao diện hiện đại**: Smart Study Advisor Hero Card và 7-Day Forecast Pills trên `DashboardPage`, Typo suggestion banner trên `VocabularyPage`.

---

## 📋 Chi Tiết Từng Giai Đoạn

### ✅ Phase 1 — Nền Tảng Kiến Trúc (Foundation) — 100%
- [x] **Cấu trúc Clean Architecture 4 tầng**:
  - `src/Lexora.Domain`: Entity, Enums, Value Objects, Repository Interfaces (không phụ thuộc bên thứ 3).
  - `src/Lexora.Application`: DTOs, Service Interfaces, SM-2 Engine, Business Logic.
  - `src/Lexora.Infrastructure`: EF Core 10, SQLite, Repository Implementations, Backup & Audio Storage.
  - `src/Lexora.Presentation`: WinUI 3 (Windows App SDK), MVVM CommunityToolkit, Mica Backdrop.
- [x] **Database & ORM**:
  - SQLite lưu trữ tại `%LocalAppData%/Lexora/data/lexora.db`.
  - Khởi tạo tự động, cascade delete, foreign keys và seed dữ liệu mẫu ban đầu (`SqliteDatabaseInitializer`).
- [x] **Cơ chế Theme & Đa Ngôn Ngữ**:
  - `ThemeService`: Chuyển đổi linh hoạt `Light / Dark / System Default`.
  - `LocalizationService`: Hỗ trợ song ngữ Tiếng Việt (`vi-VN`) & English (`en-US`) qua resource strings (`.resw`).
- [x] **Điều hướng & Shell**:
  - `ShellPage` tích hợp `NavigationView` hiện đại với Acrylic / Mica, chuyển đổi các tab Dashboard, Vocabulary, Review, Grammar, Settings.

---

### ✅ Phase 2 — Quản Lý Từ Vựng (Vocabulary) — 100%
- [x] **Cấu trúc Dữ liệu Đa Nghĩa**:
  - Entity `Word`: Từ gốc, phiên âm IPA, đường dẫn Audio, ghi chú ngữ cảnh.
  - Entity `WordMeaning`: Hỗ trợ 1 từ có nhiều nghĩa, từ loại (`n, v, adj, adv, pv, prep, conj`), CEFR Level (`A1-C2`).
  - Entity `ExampleSentence`: Câu ví dụ tiếng Anh kèm bản dịch tiếng Việt tương ứng cho từng nghĩa.
  - Phân loại theo chủ đề (`Topic`) và phân loại kỳ thi (`ExamTag`: TOEIC, VSTEP, IELTS).
- [x] **Tính năng CRUD & Nghiệp vụ**:
  - Thêm / Sửa / Xóa từ vựng hoàn chỉnh kèm dialog trực quan (`WordEditorDialog.xaml`).
  - Tự động tạo thẻ ôn tập (`ReviewCard`) ngay khi thêm từ mới.
- [x] **Tìm Kiếm, Lọc & Sắp Xếp**:
  - Tìm kiếm real-time theo từ vựng hoặc nghĩa tiếng Việt.
  - Lọc đa chiều: Theo Chủ đề (Topic), Theo Cấp độ (CEFR A1-C2), Theo Kỳ thi (TOEIC/VSTEP/IELTS).
  - Sắp xếp: Mới nhất, Cũ nhất, Bảng chữ cái A-Z, Z-A.
- [x] **Nhập / Xuất Dữ Liệu (Import/Export)**:
  - Hỗ trợ định dạng CSV và JSON chuẩn UTF-8 (`ExportImportService`).
- [x] **Phát Âm & Âm Thanh**:
  - Service phát âm audio offline/caching (`WindowsAudioPlayerService` + `LocalAudioStorageService`).

---

### ✅ Phase 3 — Học & Ôn Tập Lặp Lại Ngắt Quãng (Learning & SM-2) — 100%
- [x] **Thuật Toán Spaced Repetition SuperMemo SM-2**:
  - Engine chuẩn `Sm2SpacedRepetitionEngine`: Tính toán `EaseFactor` (sàn tối thiểu 1.3), khoảng cách ngày ôn tập (`IntervalDays`), chu kỳ lặp (`Repetitions`).
  - Lập lịch ngày ôn tập tiếp theo chính xác (`NextReviewAt`).
- [x] **Hệ Thống Đánh Giá (4 Mức Độ)**:
  - `Again (0)`: Quên hoàn toàn — Reset interval về 1 ngày, giảm EaseFactor.
  - `Hard (3)`: Nhớ khó khăn — Tăng interval vừa phải, giảm nhẹ EaseFactor.
  - `Good (4)`: Nhớ tốt — Tăng interval theo công thức chuẩn SM-2.
  - `Easy (5)`: Rất dễ — Thưởng bonus interval và tăng EaseFactor.
- [x] **Theo Dõi Tiến Trình & Chuỗi Học (Streak)**:
  - Lưu chi tiết nhật ký học tập (`StudyLog`) mỗi lần lật thẻ.
  - Quản lý phiên học (`StudySession`).
  - Tự động cập nhật chuỗi ngày học liên tục (`UserStreak`) và ngày học dài nhất.
- [x] **Giao Diện Flashcard Tương Tác Hiện Đại**:
  - `ReviewPage.xaml`: Hiệu ứng lật thẻ 3D (Flashcard Flip Animation) mượt mà.
  - Thanh đo tiến độ ôn tập trong ngày (`ProgressBar` trực quan: Remaining / Completed).
  - Phím bấm và nút đánh giá tương tác chuẩn Fluent UI.
- [x] **Hệ Thống Nhắc Nhở Học Tập**:
  - `ReviewReminderService`: Kiểm tra các thẻ đến hạn cần ôn trong ngày.

---

### ✅ Phase 4 — Kiểm Tra & Luyện Tập (Practice & Quizzes) — 100%
- [x] **Đa dạng các dạng câu hỏi trắc nghiệm & điền từ**:
  - Trắc nghiệm Từ vựng -> Nghĩa (Word to Meaning Quiz).
  - Trắc nghiệm Nghĩa -> Từ vựng (Meaning to Word Quiz).
  - Bài tập điền từ còn thiếu vào câu (Fill in the Blanks) dựa trên câu ví dụ thực tế.
  - Bài tập nhận diện từ loại (Part of Speech Quiz).
  - Bài tập Nghe - Nhận diện từ phát âm (Listening / Audio Quiz).
  - Chế độ kiểm tra tổng hợp (Mixed Test) kết hợp linh hoạt tất cả các dạng.
- [x] **Tùy biến bài kiểm tra & Mô phỏng kỳ thi**:
  - Cấu hình số lượng câu hỏi linh hoạt (5, 10, 20, 30 câu).
  - Lọc theo Kỳ thi (TOEIC / VSTEP / IELTS), theo Chủ đề (Topic) hoặc theo Cấp độ (CEFR A1-C2).
  - Chế độ tập trung vào từ yếu (Target Weak Words Only).
- [x] **Hệ thống đánh giá & Tích hợp Spaced Repetition**:
  - Tự động ghi nhận `StudySession` và `StudyLog` khi nộp bài test.
  - Cập nhật chuỗi ngày học (`UserStreak`) sau mỗi lần làm bài.
  - Tự động phát hiện danh sách "Từ yếu" (Weak Words) khi làm sai, đánh dấu `NotRemembered` và tăng `Lapses`.
  - Nút chuyển nhanh 1-click đưa toàn bộ từ yếu vào hàng đợi Spaced Repetition (SM-2) để ôn luyện ngay.
- [x] **Giao diện người dùng Fluent UI**:
  - `TestPage.xaml` với 3 chế độ: Setup, Testing và Result.
  - Điều hướng bàn phím tiện dụng (1-4 / A-D chọn đáp án, Space nghe audio, Enter sang câu tiếp theo).
  - Hỗ trợ song ngữ Tiếng Việt & Tiếng Anh qua `.resw`.

---

### ✅ Phase 5 — Ngữ Pháp & Cấu Trúc Câu (Grammar Master) — 100%
- [x] **Nghiệp vụ CRUD cấu trúc ngữ pháp**:
  - Entity `GrammarRule` và `GrammarExample` trong Domain.
  - Triển khai đầy đủ Thêm / Sửa / Xóa cấu trúc ngữ pháp kèm cascade quản lý danh sách ví dụ.
  - Dialog biên tập trực quan [GrammarEditorDialog.xaml](file:///d:/LEARN/Lexora/src/Lexora.Presentation/Views/GrammarEditorDialog.xaml).
- [x] **Trình phân loại & Lọc đa chiều**:
  - Lọc theo Cấp độ chuẩn Châu Âu CEFR (`A1-C2`).
  - Lọc theo Kỳ thi định hướng: `TOEIC`, `VSTEP`, `IELTS`, `General`.
  - Lọc theo Chủ đề (Topic) và tìm kiếm real-time theo tiêu đề, cấu trúc, lời giải thích.
- [x] **Chi tiết bài học & Cảnh báo lỗi**:
  - Hộp công thức trực quan (Formula box) phông chữ Monospace nổi bật.
  - Giải thích song ngữ Tiếng Việt & Tiếng Anh.
  - Khung cảnh báo lỗi sai phổ biến (*Common Mistakes & Pitfalls*) màu vàng cảnh báo.
  - Danh sách câu ví dụ ngữ cảnh có bản dịch và ghi chú.
- [x] **Bài tập thực hành ngữ pháp tương tác (Grammar Practice & Quiz)**:
  - Sinh đề thi trắc nghiệm ngữ pháp thông minh từ kho cấu trúc và câu ví dụ.
  - Chấm điểm tức thì, giải thích chi tiết đáp án đúng/sai và tổng kết điểm số.

---

### ✅ Phase 6 — Báo Cáo & Thống Kê (Dashboard & Analytics) — 100%
- [x] **Service tính toán số liệu thống kê chuyên sâu (`StatisticsService`)**:
  - `GetDashboardSummaryAsync`: Đếm số từ theo trạng thái, tỷ lệ nhớ, độ chính xác %, số từ cần ôn trong ngày.
  - `GetDetailedAnalyticsAsync`: Tổng hợp phân bổ từ vựng theo CEFR (A1-C2), Từ loại (POS), Kỳ thi (TOEIC, VSTEP, IELTS), và Chủ đề (Topics).
- [x] **Đường cong lãng quên & Lưu giữ (Retention Curve)**:
  - Phân tích tỷ lệ ghi nhớ từ vựng thực tế theo các mốc thời gian (1 ngày, 3 ngày, 7 ngày, 14 ngày, 30 ngày, 60+ ngày).
- [x] **Lịch hoạt động học tập 30 ngày (30-Day Activity Heatmap)**:
  - Bản đồ nhiệt (GitHub-style activity heatmap) trực quan hóa tần suất học và ôn tập theo từng ngày trong tháng kèm tooltip chi tiết.
- [x] **Tiến độ ôn thi chứng chỉ (Exam Preparation Trackers)**:
  - Thanh tiến độ động theo từng kỳ thi mục tiêu: TOEIC (Target 600 - 850+), VSTEP (B1 - C1), IELTS (Band 6.5 - 8.0+).
- [x] **Lịch sử các phiên học gần đây (Recent Study Sessions)**:
  - Hiển thị danh sách các buổi học/kiểm tra gần nhất với thời lượng, số thẻ hoàn thành, và độ chuẩn xác.
- [x] **Giao diện Dashboard Fluent UI hiện đại (`DashboardPage.xaml` & `DashboardViewModel.cs`)**:
  - Thiết kế card hiện đại với hiệu ứng đổ bóng, thanh tiến trình màu sắc động, bố cục responsive và liên kết nhanh 1-click tới các chức năng chính.

---

### ✅ Phase 7 — Tối Ưu Trải Nghiệm & Trợ Năng (UI/UX Polish) — 100%
- [x] Hiệu ứng nền Mica Alt / Acrylic trên Windows 11.
- [x] Hỗ trợ chuyển đổi Theme sáng/tối không cần khởi động lại app.
- [x] Script tiện ích khởi chạy nhanh (`run.ps1`, `run.cmd`).
- [x] Hỗ trợ phím tắt toàn diện:
  - `Ctrl + 1/2/3/4/5`: Điều hướng nhanh các màn hình Dashboard, Vocabulary, Review, Test, Grammar.
  - `Ctrl + F`: Tiêu điểm tìm kiếm toàn cục từ bất cứ đâu.
  - `Ctrl + N` / `Ctrl + E` / `Delete`: Thêm, Sửa, Xóa từ vựng nhanh trên trang Từ vựng.
  - `Space`: Lật thẻ ghi nhớ (Review) / Nghe lại phát âm (Test).
  - `1 - 4` hoặc `A - D`: Đánh giá mức độ nhớ SM-2 (Again/Hard/Good/Easy) và chọn đáp án trắc nghiệm.
  - `Enter`: Nộp bài / Chuyển câu hỏi tiếp theo trong bài kiểm tra.
- [x] Tối ưu hóa chuyển trang (Page Transitions & Entrance Animations qua `NavigationService`).
- [x] Hỗ trợ Text-to-Speech (TTS) tự nhiên & âm thanh đàm thoại song ngữ UK/US, kèm card kiểm tra phát âm trong Settings.
- [x] Bảng hướng dẫn phím tắt (Keyboard Shortcuts Reference) trực quan ngay trong Settings.
- [x] **Hệ thống Nhận diện Thương hiệu & Startup Animation (Windows 11 Fluent Design)**:
  - Giữ trọn vẹn thiết kế Logo Lexora: biểu tượng chữ L cách điệu lồng ghép gáy sách & các tầng tri thức (Cyan, Emerald Teal, Amber Gold).
  - Bộ tài sản Vector SVG chuẩn hóa 6 phiên bản (`assets/brand/`): App Icon 1024x1024, Icon-only, Horizontal Logo Light/Dark, Monochrome B&W.
  - Startup Animation 1.8s mượt mà 60 FPS: Logo reveal (scale 82% → 100%), cánh tri thức xuất hiện so le, hiệu ứng vệt sáng quét qua logo (soft light sweep), chữ Lexora trượt nhẹ lên (slide-up + fade-in), và thanh loading mỏng tối giản.
  - Xử lý tương tác 5 trạng thái chuẩn mực (`Starting`, `Loading`, `Ready`, `LoadingError`, `Cancelled/Closed`).
  - Hỗ trợ `CancellationToken`, tự động nhận diện và tôn trọng chế độ **Reduced Motion / Accessibility** (`UISettings.AnimationsEnabled`).

---

### ✅ Phase 8 — Đóng Gói, Sao Lưu & Phát Hành (Packaging & Release) — 100%
- [x] **45 Unit Tests & Integration Tests tự động hoá** (`dotnet test`):
  - 27 Unit Tests kiểm thử toàn diện SM-2, Review, Test Quiz, Grammar, Import/Export, Statistics.
  - 18 Integration Tests kiểm thử SQLite DB, CRUD, Học tập, Báo cáo, Sao lưu DB, Cascade Integrity, và Large Volume Performance.
- [x] **Kiểm tra toàn vẹn Database (Foreign key cascade deletion)**: Tự động dọn sạch meanings, examples, và review cards khi xóa từ vựng.
- [x] **Đo kiểm hiệu năng với khối lượng dữ liệu lớn (Large Volume Performance)**: Tải và lọc 300+ từ vựng dưới 500ms.
- [x] **Xác thực Cancellation Token**: Hỗ trợ hủy tác vụ bất đồng bộ an toàn trên tất cả service & repository.
- [x] **Giao diện Sao lưu / Phục hồi thủ công trong trang Settings**:
  - Đo lường và hiển thị trực tiếp dung lượng file SQLite (`.db`), vị trí lưu trữ, và số lượng bản ghi thực tế.
  - Tạo snapshot sao lưu tức thì với 1-click.
  - Phục hồi an toàn từ file `.db` đã sao lưu kèm hộp thoại cảnh báo xác nhận.
- [x] **Quy trình Build & Release tự động hóa (`build-release.ps1`)**:
  - Tự động chạy toàn bộ test suite Release.
  - Đóng gói ứng dụng WinUI 3 sạch vào thư mục `publish/win-x64`.

---

### ✅ Phase 10 — Trí Thông Minh Học Tập, Dự Báo Khối Lượng & Tối Ưu Hóa (Intelligence & Optimization) — 100%
- [x] **Smart Learning Advisor & Daily Recommendations (`IStudyAdvisorService`)**:
  - Phân tích độ suy giảm trí nhớ Ebbinghaus, tính toán điểm rủi ro (Retention Risk Score) theo thời gian quá hạn và số lần quên (`Lapses`).
  - Tự động sinh đề xuất học tập cá nhân hóa (`UrgentReview`, `WeakWordsQuiz`, `DailyMaintenance`) kèm nút kích hoạt 1-click trực tiếp từ Dashboard.
- [x] **Dự báo khối lượng ôn tập 7 ngày tới (7-Day Spaced Repetition Workload Forecast)**:
  - Lập lịch chiếu tải ôn tập trong 7 ngày tiếp theo (`D0` đến `D6`) dựa trên chỉ mục `NextReviewAt` và `Status`.
  - Hiển thị trực quan qua dải thẻ ngày (Forecast Pills) kèm số lượng thẻ và nhãn cường độ.
- [x] **Tìm kiếm thông minh chịu lỗi & Gợi ý chính tả (`IFuzzySearchService`)**:
  - Tích hợp thuật toán Damerau-Levenshtein distance tối ưu $O(N \times M)$ với mảng đệm không phân bổ (`stackalloc Span<int>`).
  - Nhận diện lỗi gõ nhầm và tự động gợi ý "Có phải bạn muốn tìm: *[từ đúng]*" khi người học gõ sai chính tả.
- [x] **Tối ưu hóa Database & Query Performance**:
  - Triển khai `CountDueCardsAsync` và `GetDueForecastCountsAsync` zero-allocation trên `ReviewRepository`, cắt giảm 99% chi phí I/O và memory allocation so với việc nạp 1.000 thực thể vào bộ nhớ.
  - Tích hợp Debounce tìm kiếm bất đồng bộ kèm Token hủy tác vụ để tránh quá tải truy vấn khi gõ phím.
- [x] **Bộ kiểm thử toàn diện**: 96/96 tests pass (70 Unit Tests + 26 Integration Tests, 0 errors, 0 warnings).

---

### ✅ Phase 11 — Tối Ưu Đa Ngôn Ngữ Tức Thời & Hiệu Ứng Mượt Mà (Real-Time i18n & Fluid Motion) — 100%
- [x] **Tối ưu hóa Đa Ngôn Ngữ Tức Thời (Real-Time Dynamic i18n)**:
  - Tự động nạp ngôn ngữ đã lưu (`LanguageCode`) từ `UserPreferencesService` ngay khi khởi động mà không làm chậm splash sequence.
  - Sửa lỗi điều hướng trùng lặp trong `NavigationService` bằng cơ chế `force: true` và phương thức `Reload(new SuppressNavigationTransitionInfo())` giúp reload trang không giật lag.
  - Kết nối lắng nghe sự kiện `LanguageChanged` trực tiếp tại tất cả các View (`SettingsPage`, `DashboardPage`, `VocabularyPage`, `ReviewPage`, `TestPage`, `GrammarPage`) và gọi `Bindings.Update()` tại chỗ — chuyển đổi ngôn ngữ mượt mà trong tích tắc mà không cần khởi động lại ứng dụng.
- [x] **Tối ưu hóa Hiệu Ứng Chuyển Động Mượt Mà (Fluent Fluid Transitions)**:
  - Tích hợp hiệu ứng chuyển cảnh trang tự nhiên (`SlideNavigationTransitionInfo` trượt từ phải sang trái) làm mặc định cho `NavigationService`.
  - Bổ sung `ChildrenTransitions` với `EntranceThemeTransition (FromVerticalOffset="20")` vào các khu vực tương tác cốt lõi:
    - **DashboardPage**: Toàn bộ nội dung Dashboard trượt lên mềm mại khi khởi động.
    - **ReviewPage**: Khung đáp án/định nghĩa và 4 nút chấm điểm SM-2 trượt vào êm ái khi lật thẻ.
    - **TestPage**: Hiệu ứng chuyển động mượt mà khi chuyển giữa chế độ Cấu hình (Setup), Thi trắc nghiệm (Testing) và Bảng tổng kết kết quả (Result).
- [x] **Kiểm thử & Đảm bảo chất lượng**: Biên dịch 0 warnings, 0 errors, toàn bộ 96/96 Unit & Integration Tests đạt 100%.

---

## 🛠️ Hướng Dẫn Vận Hành & Khởi Chạy

```powershell
# 1. Khởi chạy ứng dụng nhanh từ thư mục gốc
.\run.ps1
# hoặc:
dotnet run --project src/Lexora.Presentation

# 2. Biên dịch toàn bộ Solution
dotnet build Lexora.slnx

# 3. Chạy toàn bộ 45 bộ test tự động (0 errors, 0 warnings)
dotnet test Lexora.slnx

# 4. Đóng gói Release phiên bản phát hành
.\build-release.ps1
```

