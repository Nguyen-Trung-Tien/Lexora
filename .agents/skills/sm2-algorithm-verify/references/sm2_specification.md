# 🧠 Spaced Repetition (SM-2) Engine Specification

## 1. Algorithm Overview
Lexora uses the SuperMemo-2 (SM-2) algorithmic foundation implemented in `Sm2SpacedRepetitionEngine` (`Lexora.Application.Services`).

## 2. Mathematical Definition

### 2.1 Rating Scale ($q$)
- **`0`**: Complete blackout; complete failure to recall.
- **`1`**: Incorrect response; correct term remembered upon reveal.
- **`2`**: Correct response with significant hesitation / difficulty (Hard).
- **`3`**: Correct response with standard effort / good recall (Good).
- **`4`**: Perfect, instantaneous recall (Easy).

### 2.2 Ease Factor ($EF$) Calculation
$$EF' = EF + (0.1 - (4 - q) \times (0.08 + (4 - q) \times 0.02))$$

> [!IMPORTANT]
> **Ease Factor Floor Invariant**:
> $$EF' = \max(EF', 1.3)$$
> Under no circumstances may $EF$ drop below $1.3$.

### 2.3 Repetitions ($n$) & Interval ($I$) Calculation
1. **Lapse ($q < 2$)**:
   - $n = 0$
   - $I = 1\text{ day}$
2. **Success ($q \ge 2$)**:
   - $n' = n + 1$
   - Interval rules:
     - If $n' = 1$: $I = 1\text{ day}$
     - If $n' = 2$: $I = 6\text{ days}$
     - If $n' > 2$: $I = \lceil I_{\text{prev}} \times EF' \rceil\text{ days}$

## 3. Concurrency & Mutation Safety
- Review submissions must create a `StudyLog` entry, update the Word's interval, repetitions, ease factor, and due date atomically.
- Card rating buttons must use concurrency flags (`IsSubmitting`) to prevent double-click submissions.
