### Added
- [Pronunciation & Listening Lab]: Implemented end-to-end `IPronunciationListeningLabService`, `PronunciationListeningLabService`, and `ListeningLabDialog` featuring Audio Dictation with Levenshtein grading, Minimal Pairs phoneme discrimination (/iː/ vs /ɪ/, /æ/ vs /e/, /θ/ vs /s/, etc.) with articulatory guides, and UK vs US side-by-side accent comparison.
- [Automated Tests]: Added 13 new unit tests in `PronunciationListeningLabServiceTests.cs`, raising total passing test suite to 378/378 tests.

### Fixed
- [Fluent 2 Design Compliance]: Replaced hardcoded hex colors in `App.xaml` by consolidating HeatmapLevel brushes into `Themes/DesignSystem.xaml`, and eliminated hardcoded alpha hex strings `#00000000` with `Transparent` in `SplashScreenOverlay.xaml` (100% compliant with zero audit violations).
- [Bilingual Parity & Localization]: Added 32 missing localization keys across Vietnamese (`vi-VN`) and English (`en-US`) for Learning Intelligence, CAT Adaptive Quiz, Memory Stability, and Listening Lab, achieving 100% dictionary and XAML reference parity (1,173 keys).
- [Audio MIME Type Handling]: Updated `WindowsAudioPlayerService` to dynamically detect `.mp3` vs `.wav` cached human audio streams.
