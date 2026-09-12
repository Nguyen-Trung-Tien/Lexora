# 📊 Lexora — Tiến Độ Phát Triển Dự Án (Project Progress Tracker)

> Cập nhật lần cuối: **12/09/2026** | Phiên bản hiện tại: **0.3.0-alpha**  
> Nền tảng: **.NET 10** • **WinUI 3 (Windows App SDK)** • **SQLite / EF Core 10** • **Clean Architecture**

---

## 🚀 Tổng Quan Tiến Độ (Dashboard)

| Giai Đoạn | Tên Module / Phase | Trạng Thái | Tiến Độ (%) | Kiểm Thử |
|---|---|:---:|:---:|:---:|
| **Phase 1** | **Foundation & Clean Architecture** | ✅ Hoàn thành | 100% | Đã verify |
| **Phase 2** | **Vocabulary Management (CRUD, Audio, Import/Export)** | ✅ Hoàn thành | 100% | 10/10 tests pass |
| **Phase 3** | **Learning & Spaced Repetition (SM-2 Algorithm)** | ✅ Hoàn thành | 100% | 9/9 tests pass |
| **Phase 4** | **Vocabulary Test & Practice (Quiz, Dictation, Fill-in)** | ⏳ Chưa bắt đầu | 0% | Chờ triển khai |
| **Phase 5** | **Grammar & Sentence Structure** | 🔄 Một phần | 35% | Models & UI cơ bản |
| **Phase 6** | **Dashboard, Analytics & Retention Charts** | 🔄 Một phần | 40% | Service & DTOs sẵn sàng |
| **Phase 7** | **UI/UX Polish, Shortcuts & Accessibility** | 🔄 Một phần | 40% | Mica, Themes, Resw sẵn sàng |
| **Phase 8** | **Backup, Packaging & Release** | 🔄 Một phần | 35% | Backup Service & DB test pass |

**Tổng tiến độ toàn diện toàn dự án: ~45%**  
**Trạng thái Build & Test**: 19/19 Unit & Integration Tests **PASS** (0 errors, 0 warnings).

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

### ⏳ Phase 4 — Kiểm Tra & Luyện Tập (Practice & Quizzes) — 0%
- [ ] Trắc nghiệm Từ vựng -> Nghĩa (Word to Meaning Quiz).
- [ ] Trắc nghiệm Nghĩa -> Từ vựng (Meaning to Word Quiz).
- [ ] Bài tập điền từ còn thiếu vào câu (Fill in the Blanks).
- [ ] Bài tập Nghe - Chép chính tả (Dictation / Audio Quiz).
- [ ] Mini-test định dạng mô phỏng kỳ thi TOEIC / VSTEP.

---

### 🔄 Phase 5 — Ngữ Pháp & Cấu Trúc Câu (Grammar Master) — 35%
- [x] Entity `GrammarRule` và `GrammarExample` trong Domain.
- [x] `GrammarRepository` và `GrammarService` cơ bản.
- [x] Giao diện danh mục ngữ pháp sơ bộ (`GrammarPage.xaml`).
- [ ] Trình phân loại ngữ pháp theo CEFR và chủ đề chuyên sâu.
- [ ] Cảnh báo lỗi sai phổ biến (Common Mistakes & Pitfalls).
- [ ] Bài tập thực hành ngữ pháp tương tác.

---

### 🔄 Phase 6 — Báo Cáo & Thống Kê (Dashboard & Analytics) — 40%
- [x] Service tính toán số liệu thống kê (`StatisticsService`).
- [x] DTOs tóm tắt (`DashboardSummaryDto`): Số từ đã thuộc, số từ đang học, streak hiện tại, độ chuẩn xác %.
- [x] Giao diện Dashboard tổng quan (`DashboardPage.xaml`).
- [ ] Biểu đồ trực quan hoá khả năng ghi nhớ & đường cong lãng quên (Forgetting Curve).
- [ ] Biểu đồ phân bổ từ vựng theo kỳ thi TOEIC / VSTEP / CEFR.
- [ ] Lịch heatmap điểm danh chuỗi học tập (Study Heatmap).

---

### 🔄 Phase 7 — Tối Ưu Trải Nghiệm & Trợ Năng (UI/UX Polish) — 40%
- [x] Hiệu ứng nền Mica Alt / Acrylic trên Windows 11.
- [x] Hỗ trợ chuyển đổi Theme sáng/tối không cần khởi động lại app.
- [x] Script tiện ích khởi chạy nhanh (`run.ps1`, `run.cmd`).
- [ ] Hỗ trợ phím tắt toàn diện (Space để lật thẻ, phím 1-4 để chấm điểm SM-2).
- [ ] Tối ưu hóa chuyển trang (Page Transitions & Micro-animations).
- [ ] Hỗ trợ chế độ tương phản cao (High Contrast) và Text-to-Speech (TTS) tự nhiên.

---

### 🔄 Phase 8 — Đóng Gói, Sao Lưu & Phát Hành (Packaging & Release) — 35%
- [x] 19 Unit Tests & Integration Tests tự động hoá (`dotnet test`).
- [x] Service sao lưu & phục hồi database SQLite (`DatabaseBackupService`).
- [ ] Giao diện Sao lưu / Phục hồi thủ công trong trang Settings.
- [ ] Đóng gói cài đặt ứng dụng (MSIX / MSI / Inno Setup Portable).
- [ ] Tự động cập nhật phiên bản (Auto-updater).

---

## 🛠️ Hướng Dẫn Vận Hành & Khởi Chạy

```powershell
# 1. Khởi chạy ứng dụng nhanh từ thư mục gốc
.\run.ps1
# hoặc:
dotnet run --project src/Lexora.Presentation

# 2. Biên dịch toàn bộ Solution
dotnet build Lexora.slnx

# 3. Chạy toàn bộ 19 bộ test tự động
dotnet test Lexora.slnx
```
