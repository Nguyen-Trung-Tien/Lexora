# 🗺️ Lexora Strategic Roadmap & Product Architecture

> **Lexora** is a high-performance, offline-first English learning and vocabulary management desktop application designed for TOEIC, IELTS, and VSTEP exam preparation. Built with **.NET 10**, **WinUI 3 / Windows App SDK**, and **Entity Framework Core 10 (SQLite)** following strict **Clean Architecture** patterns.

---

## 📌 Phân kỳ chiến lược phát triển (Phased Strategy)

```mermaid
gantt
    title Lexora Architecture & Engineering Roadmap
    dateFormat  YYYY-MM
    section Phase 1: MVP & Core Architecture
    Clean Architecture Foundation (4 Layers) :done, 2026-01, 2026-04
    SM-2 Spaced Repetition & SQLite WAL     :done, 2026-03, 2026-06
    343 Unit & Integration Test Suite        :done, 2026-05, 2026-07
    section Phase 2: Production Hardening & UX
    FSRS v5 Memory Engine Integration        :done, 2026-08, 2026-09
    Diacritic RAM & Memory Leak Elimination  :done, 2026-09, 2026-10
    3D Flashcard Physics & Fluent 2 Polish  :active, 2026-10, 2026-11
    CI/CD DevSecOps & Skill Automation      :active, 2026-10, 2026-12
    section Phase 3: Autonomous Mesh & Cloud
    Encrypted Cloud Sync (WebDAV / P2P)      :2027-01, 2027-04
    Offline On-Device AI Assessment (SLM)    :2027-04, 2027-07
    Advanced Learning Analytics & Heatmaps   :2027-07, 2027-09
```

---

## 🎯 Chi tiết từng phân kỳ (Phase Breakdown)

### 🟢 Phase 1: MVP & Core Clean Architecture (Hoàn thành)
- [x] **Strict 4-Layer Clean Architecture**: Phân tách `Lexora.Domain`, `Lexora.Application`, `Lexora.Infrastructure`, `Lexora.Presentation`.
- [x] **Spaced Repetition (SM-2)**: Thuật toán SuperMemo-2 với Ease Factor floor ($EF \ge 1.3$) và study logs tracking.
- [x] **SQLite Database Engine**: Lưu trữ offline `%LocalAppData%/Lexora/data/lexora.db` kích hoạt chế độ WAL (Write-Ahead Logging).
- [x] **Quality Gate Benchmark**: Đạt **343/343 Automated Tests** (292 Unit Tests + 51 Integration Tests) với 0 Warning, 0 Error.
- [x] **Bilingual Localization (i18n)**: Hỗ trợ chuyển đổi tức thời giữa Tiếng Việt (`vi-VN`) và English (`en-US`) qua `ILocalizationService`.

### 🟡 Phase 2: Production Hardening & Advanced UX (Đang triển khai)
- [x] **Free Spaced Repetition Scheduler (FSRS v5)**:
  - Cài đặt `IFsrsSpacedRepetitionEngine` mô hình hóa độ nhớ qua 3 tham số $S$ (Stability), $D$ (Difficulty), $R$ (Retrievability).
  - Khả năng tương thích ngược hoàn toàn với dữ liệu SM-2 lịch sử.
- [x] **Critical Resilience & RAM Optimization**:
  - Triệt tiêu Memory Leak từ anonymous lambda event handlers trên toàn bộ các View.
  - Tối ưu `SearchDiacriticFallbackAsync`: Chuyển từ full-table load sang server-side projection, giảm 85% bộ nhớ RAM tiêu thụ khi tìm kiếm từ vựng tiếng Việt có dấu.
- [x] **Flashcard 3D Spatial Physics**:
  - Trải nghiệm lật thẻ 3D (`PlaneProjection`) với góc lật mượt mà 300ms inflection point tại 90°.
- [x] **Responsive Layout Adaptability**:
  - Tự động chuyển đổi layout Review từ 2 cột sang 1 cột xếp chồng khi màn hình dưới 880px, chống clipping nút bấm.
- [ ] **Automated CI/CD Quality Gates**:
  - Thiết lập GitHub Actions tự động kiểm thử định dạng CHANGELOG và chạy Skill Smoke Tests.

### 🔵 Phase 3: Autonomous Mesh, Cloud & AI Extensions (Kế hoạch tương lai)
- [ ] **End-to-End Encrypted Sync**: Đồng bộ hóa dữ liệu học qua WebDAV / Nextcloud hoặc giao thức P2P an toàn.
- [ ] **On-Device Small Language Model (SLM)**: Tích hợp mô hình ngôn ngữ nhỏ (Phi-3 / Gemma-2B ONNX Runtime) chạy offline để tự động tạo câu ví dụ và giải thích ngữ pháp theo ngữ cảnh.
- [ ] **AI Pronunciation & Speech Assessment**: Chấm điểm phát âm IPA theo thời gian thực sử dụng Windows Media Audio Input & Whisper.cpp offline.
- [ ] **Multi-Profile Support**: Hỗ trợ nhiều người học trên cùng một máy tính với database vault riêng biệt.
