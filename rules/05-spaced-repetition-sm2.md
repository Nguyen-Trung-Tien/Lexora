# 🧠 Rule 05: Spaced Repetition (SM-2) Integrity

---

## 1. Engine Invariants

All spaced repetition interval calculations, ease factor updates, and review scheduling **MUST** be routed exclusively through `ISpacedRepetitionEngine` (`Sm2SpacedRepetitionEngine`).

### Mathematical Boundaries:
1. **Ease Factor Floor ($EF \ge 1.3$)**:
   - The Ease Factor ($EF$) must never drop below **1.3**.
   - Formula:
     $$EF' = \max\left(1.3,\; EF + \left(0.1 - (5 - q) \cdot (0.08 + (5 - q) \cdot 0.02)\right)\right)$$
   - Clamping to 1.3 prevents "Ease Hell" where cards become stuck with 1-day intervals indefinitely.

2. **Interval Calculation**:
   - Consecutive successful repetitions ($n$):
     $$I(n) = \begin{cases}
     1 \text{ day}, & n = 1 \\
     6 \text{ days}, & n = 2 \\
     \text{Round}(I(n - 1) \cdot EF'), & n > 2
     \end{cases}$$

3. **Lapse Protocol (`Again` / $q=1$)**:
   - When a user rates a card as `Again`:
     - Repetitions ($n$) resets to `0`.
     - Interval resets to `1` day (`NextReviewAt = Today + 1 day`).
     - Lapse counter increments by `+1`.
     - Status resets to `Learning`.

---

## 2. Review Submission & Streak Contract

1. **Logging Requirement**:
   - Every graded review must append an immutable `StudyLog` record capturing:
     - `ReviewCardId`
     - `Rating` ($1, 2, 4, 5$)
     - `ResponseTimeMs`
     - Timestamp (`ReviewedAt`)
     - Snapshot of interval and ease factor after rating.
2. **Daily Streak Progression**:
   - Every successful card review must trigger `IReviewRepository.UpdateStreakAsync()`.
   - Rules:
     - If last review was **yesterday**, increment `CurrentStreak += 1`.
     - If last review was **today**, retain `CurrentStreak` without increment.
     - If last review was older than yesterday, reset `CurrentStreak = 1`.
     - Update `LongestStreak = Math.Max(LongestStreak, CurrentStreak)`.

---

## 3. UI Concurrency & Double-Click Guards

In `ReviewViewModel` and `ReviewPage`:
- Users frequently double-click rating buttons or press keyboard shortcuts rapidly (<kbd>1</kbd>, <kbd>2</kbd>, <kbd>3</kbd>, <kbd>4</kbd>).
- The ViewModel **MUST** enforce a boolean concurrency lock (`_isRatingInProgress`) surrounding the rating logic:
  ```csharp
  if (_isRatingInProgress) return;
  _isRatingInProgress = true;
  try
  {
      await SubmitRatingAsync(rating);
  }
  finally
  {
      _isRatingInProgress = false;
  }
  ```
- Failure to enforce this lock causes race conditions, resulting in duplicate `StudyLog` rows and corrupted SM-2 intervals.
