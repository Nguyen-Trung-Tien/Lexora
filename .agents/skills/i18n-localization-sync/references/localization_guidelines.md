# 🌐 Lexora i18n Localization Guidelines

## 1. Architecture & Dual-Dictionary Model
Lexora uses an offline, in-memory dual-dictionary localization service:
- **Service Interface**: `ILocalizationService`
- **Implementation**: `LocalizationService` (registered as `Singleton` in DI)
- **Supported Cultures**:
  - `vi-VN`: Tiếng Việt (Default)
  - `en-US`: English (US)

## 2. Key Naming Scheme
All keys follow the `Category_Descriptor` PascalCase standard:

| Category Prefix | Purpose | Example Keys |
| :--- | :--- | :--- |
| `Nav_` | Shell Navigation items | `Nav_Dashboard`, `Nav_Vocabulary`, `Nav_Review` |
| `Action_` | Generic user action verbs | `Action_Save`, `Action_Cancel`, `Action_Delete` |
| `Vocab_` | Vocabulary management UI | `Vocab_AddWord`, `Vocab_TermPlaceholder`, `Vocab_Difficulty` |
| `Review_` | Spaced review card UI | `Review_RatingAgain`, `Review_RatingGood`, `Review_FlipCard` |
| `Grammar_` | Grammar & syntax rule UI | `Grammar_RuleTitle`, `Grammar_LinkedVocab`, `Grammar_RelatedTitle` |
| `Test_` | Practice & exam testing UI | `Test_Submit`, `Test_QuestionCount`, `Test_Score` |
| `Settings_` | User preferences & config | `Settings_Theme`, `Settings_Language`, `Settings_Backup` |
| `Toast_` | Transient alert notifications | `Toast_SuccessTitle`, `Toast_ErrorTitle` |
| `Dialog_` | Modal confirmation text | `Dialog_DeleteWordTitle`, `Dialog_DeleteWordConfirm` |

## 3. Real-Time Language Switching
When the language changes:
1. `SetLanguage(langCode)` updates `CultureInfo.CurrentCulture` and `CultureInfo.CurrentUICulture`.
2. The `LanguageChanged` event fires.
3. Views and Shell automatically rebind, and `ContentFrame` re-navigates without restarting the desktop application.
