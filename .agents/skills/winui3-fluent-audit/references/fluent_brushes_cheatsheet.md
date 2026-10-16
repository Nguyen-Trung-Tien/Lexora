# 🎨 Fluent 2 Theme Resource Brushes Cheatsheet

This cheatsheet provides the definitive mapping from visual intent to official WinUI 3 / Windows App SDK Fluent 2 `{ThemeResource}` brushes.

## 1. Surface & Background Brushes

| Visual Intent | Forbidden Hardcoded Example | Mandatory Fluent Resource Brush |
| :--- | :--- | :--- |
| Main Page / Canvas Background | `#F3F3F3` / `#202020` | `{ThemeResource LayerFillColorDefaultBrush}` |
| Alt / Nested Page Background | `#EEEEEE` / `#181818` | `{ThemeResource LayerFillColorAltBrush}` |
| Primary Card Surface | `#FFFFFF` / `#2C2C2C` | `{ThemeResource CardBackgroundFillColorDefaultBrush}` |
| Secondary / Subtle Card Surface | `#F9F9F9` / `#1F1F1F` | `{ThemeResource SubtleFillColorSecondaryBrush}` |
| Tertiary Card Surface | `#F0F0F0` / `#141414` | `{ThemeResource SubtleFillColorTertiaryBrush}` |
| Flyout / Modal Background | `#FFFFFF` / `#2D2D2D` | `{ThemeResource SolidBackgroundFillColorBaseBrush}` |

---

## 2. Text & Content Brushes

| Visual Intent | Forbidden Hardcoded Example | Mandatory Fluent Resource Brush |
| :--- | :--- | :--- |
| Primary High-Contrast Text | `#000000` / `#FFFFFF` | `{ThemeResource TextFillColorPrimaryBrush}` |
| Secondary / Subtitle Text | `#666666` / `#A0A0A0` | `{ThemeResource TextFillColorSecondaryBrush}` |
| Tertiary / Hint / Placeholder | `#8A8A8A` / `#6E6E6E` | `{ThemeResource TextFillColorTertiaryBrush}` |
| Disabled Text | `#CCCCCC` / `#444444` | `{ThemeResource TextFillColorDisabledBrush}` |
| Accent Colored Text | `#0078D4` / `#60CDFF` | `{ThemeResource AccentTextFillColorPrimaryBrush}` |

---

## 3. Borders & Dividers

| Visual Intent | Forbidden Hardcoded Example | Mandatory Fluent Resource Brush |
| :--- | :--- | :--- |
| Default Card Outline / Stroke | `#E5E5E5` / `#383838` | `{ThemeResource CardStrokeColorDefaultBrush}` |
| Subtle Border / Inner Divider | `#F0F0F0` / `#282828` | `{ThemeResource DividerStrokeColorDefaultBrush}` |
| Control Stroke (Button/Input) | `#D1D1D1` / `#404040` | `{ThemeResource ControlStrokeColorDefaultBrush}` |

---

## 4. Status, Semantic & Feedback Brushes

| Visual Intent | Forbidden Hardcoded Example | Mandatory Fluent Resource Brush |
| :--- | :--- | :--- |
| Success / Mastered State | `#107C41` / `#DFF6DD` | `{ThemeResource SystemFillColorSuccessBrush}` |
| Success Background Tint | `#EBF9EB` / `#0E431E` | `{ThemeResource SystemFillColorSuccessBackgroundBrush}` |
| Caution / Warning State | `#7A5B00` / `#FFF4CE` | `{ThemeResource SystemFillColorCautionBrush}` |
| Caution Background Tint | `#FFF9E6` / `#43350E` | `{ThemeResource SystemFillColorCautionBackgroundBrush}` |
| Critical / Error / Danger State | `#C50F1F` / `#FDE7E9` | `{ThemeResource SystemFillColorCriticalBrush}` |
| Critical Background Tint | `#FDF3F4` / `#441014` | `{ThemeResource SystemFillColorCriticalBackgroundBrush}` |
| Neutral / Info State | `#0078D4` / `#E6F2FB` | `{ThemeResource SystemFillColorAttentionBrush}` |
