# Lexora Brand Identity & App Icon Guidelines

Bộ nhận diện thương hiệu và hướng dẫn quy chuẩn thiết kế Icon/Logo cho ứng dụng **Lexora** (Windows 11 Desktop — C# / WinUI 3).

---

## 1. Ý Nghĩa Biểu Tượng & Triết Lý Thiết Kế (Concept & Rationale)

Biểu tượng **Lexora** là sự kết tinh giữa **ngôn ngữ (Lexicon)**, **học tập (Learning)** và **tri thức thăng hoa (Aurora / Elevation)**.

### Các thành tố thị giác cốt lõi:
1. **Chữ "L" cách điệu (Monogram)**:
   - Dáng chữ **L** uốn lượn mềm mại như gáy sách và trang mở phía trước, khẳng định nhận diện tên gọi **Lexora**.
   - Gam màu trắng tuyết viền xanh băng tạo chiều sâu nổi khối (Fluent Material), tượng trưng cho sự trong sáng, rõ ràng và nền tảng vững chắc.
2. **Các lớp cánh tri thức (Knowledge Wings / Sprouting Pages)**:
   - 3 tầng cánh vươn cao tượng trưng cho các cấp độ phát triển ngôn ngữ (TOEIC, VSTEP, Master):
     - **Cyan Layer** (`#38BDF8`): Sự nhạy bén, công nghệ và khả năng tư duy logic.
     - **Emerald Teal Layer** (`#34D399`): Sự sinh trưởng của vốn từ, tính ổn định và trí nhớ dài hạn (Spaced Repetition).
     - **Amber Gold Layer** (`#FBBF24`): Thành quả, sự tự tin và phản xạ giao tiếp tự nhiên.
3. **Ngôi sao / Ánh sáng tri thức (Lexora Sparkle)**:
   - Biểu tượng 4 cánh (✦) điểm xuyết trong chữ "o" và đỉnh biểu tượng thể hiện khoảnh khắc "Aha!" khi ghi nhớ một từ vựng mới.

---

## 2. Bảng Màu Thương Hiệu (Color Palette)

| Vai trò | Tên màu | Hex Code | RGB | Ý nghĩa ứng dụng |
| :--- | :--- | :--- | :--- | :--- |
| **Primary Brand** | Royal Indigo | `#2563EB` → `#1E1B4B` | `37, 99, 235` | Nền squircle, header, màu chủ đạo ứng dụng WinUI 3 |
| **Accent 1** | Sky Cyan | `#38BDF8` | `56, 189, 248` | Điểm nhấn icon, link, trạng thái active, badge |
| **Accent 2** | Emerald Teal | `#10B981` | `16, 185, 129` | Streak học tập, từ vựng đã thuộc, tỉ lệ ghi nhớ cao |
| **Accent 3** | Warm Amber | `#F59E0B` | `245, 158, 11` | XP thưởng, huy hiệu, level, thẻ từ cần ôn tập gấp |
| **Neutral Dark** | Midnight Slate | `#0F172A` | `15, 23, 42` | Nền Dark Mode (Mica/Acrylic), typography Light Mode |
| **Neutral Light** | Crisp White | `#FFFFFF` | `255, 255, 255` | Điểm nhấn nổi khối Fluent, typography Dark Mode |

---

## 3. Hệ Thống Typography Thương Hiệu

- **Chữ thương hiệu (Wordmark)**: `Segoe UI Variable Display` (Windows 11 Native) hoặc `Outfit` / `Inter` / `Plus Jakarta Sans`.
- **Đặc trưng chữ Lexora**:
  - Font Sans-serif hiện đại với bo góc nhẹ tinh tế (Soft rounded geometric).
  - Trọng lượng (Font Weight): **Bold (700)** cho Wordmark chính, **Semi-Bold (600)** cho Tagline.
  - Chữ "o" cách điệu có thể chứa tâm 4 cánh (✦) hoặc giữ clean ở kích thước hiển thị nhỏ.
  - Tagline phụ trợ: `INTELLIGENT VOCABULARY & GRAMMAR` (Tracking +150, Upper-case).

---

## 4. Bộ 6 Phiên Bản Nhận Diện (Deliverables)

| STT | Phiên bản | Tệp tin vector / render | Phạm vi sử dụng tối ưu |
| :--- | :--- | :--- | :--- |
| 1 | **Primary App Icon** | `lexora-app-icon.svg`<br>`lexora-app-icon-1024.jpg` | Icon Desktop Shortcut, Windows Store, Microsoft Store Listing, Splash Screen, Installer Setup banner |
| 2 | **Icon-Only** | `lexora-icon-only.svg` | Windows 11 Taskbar (`Square44x44Logo.targetsize-24_altform-unplated.png`), App TitleBar, In-App Header |
| 3 | **Logo + Lexora** | `lexora-logo-full-light.svg`<br>`lexora-logo-full-light.jpg` | Banner tài liệu, GitHub README, Website, chứng chỉ học tập, hoá đơn/báo cáo |
| 4 | **Dark Background Version**| `lexora-logo-full-dark.svg`<br>`lexora-logo-dark.jpg` | Giao diện WinUI 3 Dark Mode, GitHub dark theme, Presentation cover |
| 5 | **Light Background Version**| `lexora-logo-full-light.svg` | Giao diện WinUI 3 Light Mode, in ấn văn phòng phẩm, tài liệu Word/PDF |
| 6 | **Monochrome Version** | `lexora-logo-monochrome.svg` | Khắc laser, in ấn đơn sắc 1 màu (B&W), tem nhãn watermark, fax/stencil |

---

## 5. Quy Chuẩn Kích Thước Biểu Tượng Ứng Dụng Windows

Để ứng dụng WinUI 3 hiển thị sắc nét tuyệt đối trên mọi độ phân giải DPI (100%, 125%, 150%, 200%, 400%) và không bao giờ bị vỡ hạt hay mờ mép:

1. **Tỉ lệ khung hình và Padding**:
   - Khung hình chuẩn: **1024 × 1024 px**.
   - Vùng an toàn (Safe Area Padding): **14% - 16%** từ mép ngoài vào tâm.
   - Bán kính bo góc Squircle: **`rx="220" ry="220"`** (~21.5% kích thước cạnh) đúng chuẩn Windows 11 Fluent App Tile.
2. **Kích thước xuất file cần thiết cho Windows AppX / MSIX**:
   - `AppIcon.ico`: Chứa đa tầng kích thước (16x16, 24x24, 32x32, 48x48, 64x64, 128x128, 256x256 px).
   - `Square44x44Logo.scale-100/150/200/400.png`: Icon danh sách App trong Start Menu.
   - `Square44x44Logo.targetsize-16/24/32/48_altform-unplated.png`: Icon trên thanh Taskbar Windows 11 (dùng phiên bản *Icon-Only* với nền trong suốt để hòa hợp hoàn hảo với hiệu ứng Mica của Taskbar).
   - `Square150x150Logo.scale-100/200/400.png`: Ô Tile Start Menu.
   - `SplashScreen.scale-200.png`: Màn hình khởi động ứng dụng (kết hợp nền Indigo tối và biểu tượng chính).
