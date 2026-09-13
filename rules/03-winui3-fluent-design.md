# 🎨 Rule 03: WinUI 3 & Fluent UI/UX Guidelines

---

## 1. Zero Hardcoded Colors Invariant

**Absolute Rule**: Developers and AI agents **MUST NEVER** hardcode raw hex color codes (such as `#FFF4CE`, `#E6F2FB`, `#323130`, `#FFFFFF`, `#000000`) for visual backgrounds, borders, or text elements in XAML or code-behind.

### Fluent `{ThemeResource ...}` Mapping Reference:

| Intent / Usage | Forbidden Hex Example | Mandatory Fluent Resource Brush |
| :--- | :--- | :--- |
| Primary Page Background | `#F3F3F3` / `#202020` | `{ThemeResource LayerFillColorDefaultBrush}` |
| Card / Container Surface | `#FFFFFF` / `#2C2C2C` | `{ThemeResource CardBackgroundFillColorDefaultBrush}` |
| Card Secondary Surface | `#F9F9F9` / `#1F1F1F` | `{ThemeResource CardBackgroundFillColorSecondaryBrush}` |
| Card Border / Stroke | `#E5E5E5` / `#383838` | `{ThemeResource CardStrokeColorDefaultBrush}` |
| Primary Text | `#000000` / `#FFFFFF` | `{ThemeResource TextFillColorPrimaryBrush}` |
| Secondary / Muted Text | `#666666` / `#A0A0A0` | `{ThemeResource TextFillColorSecondaryBrush}` |
| Accent Colored Text | `#0078D4` / `#60CDFF` | `{ThemeResource AccentTextFillColorPrimaryBrush}` |
| Caution / Warning State | `#FFF4CE` / `#7A5B00` | `{ThemeResource SystemFillColorCautionBrush}` |
| Success / Mastered State | `#DFF6DD` / `#107C41` | `{ThemeResource SystemFillColorSuccessBrush}` |
| Critical / Error State | `#FDE7E9` / `#C50F1F` | `{ThemeResource SystemFillColorCriticalBrush}` |

**Rationale**: Hardcoded hex colors completely break High-Contrast themes and cause unreadable text when the user toggles between Windows Light and Dark modes.

---

## 2. Responsive Layouts & Viewport Adaptability

1. **No High Fixed Widths on Cards & Dialogs**:
   - Do **NOT** specify high fixed widths (e.g. `Width="680"`, `Width="800"`) on cards, flyouts, or dialog contents.
   - Use `MaxWidth="600"` with `HorizontalAlignment="Stretch"` to allow smooth resizing on small laptop displays (1366x768) and portrait windows.
2. **ItemsRepeater vs. Fixed Lists**:
   - For multi-card layouts (Vocabulary grid, Topic collections, Grammar catalog), use `ItemsRepeater` with a `UniformGridLayout` instead of horizontal `StackPanel` or fixed column count:
     ```xml
     <ItemsRepeater ItemsSource="{x:Bind ViewModel.Words, Mode=OneWay}">
         <ItemsRepeater.Layout>
             <UniformGridLayout MinItemWidth="280" MinItemHeight="160"
                                MinRowSpacing="12" MinColumnSpacing="12"
                                ItemsStretch="Fill" />
         </ItemsRepeater.Layout>
     </ItemsRepeater>
     ```
3. **Scroll Isolation**:
   - Always wrap tall content in a `ScrollViewer` with `VerticalScrollBarVisibility="Auto"` to prevent clipping below screen bounds.

---

## 3. Dialog Architecture & Asynchronous Safety

When presenting a `ContentDialog`:

1. **Set `XamlRoot` Explicitly**:
   - Every `ContentDialog` requires its `XamlRoot` to match the active window's visual tree root before invocation:
     ```csharp
     var dialog = new WordEditorDialog(viewModel)
     {
         XamlRoot = this.XamlRoot
     };
     ```
2. **Top-Anchored Validation Feedback (`InfoBar`)**:
   - Validation error messages must be rendered via an `InfoBar` positioned at the **very top** of the dialog layout, so errors are immediately visible without scrolling.
3. **Sequential Dialog Transition Invariant**:
   - WinUI 3 throws an `InvalidOperationException` (*An async operation was already in progress*) if a dialog is shown while another dialog is closing.
   - **Mandatory fix**: Always yield the UI message loop with `await Task.Yield();` between successive dialog openings:
     ```csharp
     await firstDialog.ShowAsync();
     await Task.Yield(); // Crucial: allows previous dialog to cleanly detach from visual tree
     await secondDialog.ShowAsync();
     ```

---

## 4. Input Debouncing & UI Responsiveness

- All live search inputs (e.g., in `VocabularyPage` and `GrammarPage`) must use a 250ms debounce mechanism (via `DispatcherTimer` or Rx throttle) coupled with a `CancellationTokenSource`.
- Rapid keystrokes must cancel previous running queries to prevent UI stutter and database contention.
