# 🧠 Spaced Repetition Engine (SuperMemo SM-2) Documentation

---

## 1. Theoretical Foundation

Human memory decay adheres to the **Ebbinghaus Forgetting Curve**, which describes the exponential loss of learned information over time without reinforcement:

$$R(t) = e^{-\frac{t}{S}}$$

Where:
- $R$: Memory retrievability (probability of recall).
- $t$: Time elapsed since last review.
- $S$: Memory stability (strength of the memory trace).

The **Spaced Repetition System (SRS)** strategically schedules reviews just as memory retrievability drops to approximately 85–90%, maximizing reinforcement efficiency while minimizing study time.

Lexora implements an optimized, mathematically verified version of the **SuperMemo SM-2** algorithm designed by Piotr Woźniak.

---

## 2. Mathematical Specification

### 2.1. Ease Factor ($EF$) Calculation
The Ease Factor reflects the intrinsic difficulty of a flashcard. A higher $EF$ causes intervals to grow more rapidly.

$$EF' = \max\left(1.3,\; EF + \left(0.1 - (5 - q) \cdot (0.08 + (5 - q) \cdot 0.02)\right)\right)$$

Where:
- $EF$: Current Ease Factor (initial default is $2.5$).
- $q$: Response quality rating ($q \in \{1, 2, 4, 5\}$).
- **Hard Floor Invariant**: $EF'$ is strictly clamped to $\ge 1.3$. This prevents difficult cards from entering "Ease Hell" (where intervals never increase).

### 2.2. Interval ($I$) Progression
The interval between reviews is calculated based on consecutive successful repetitions ($n$):

$$I(n) = \begin{cases} 
1 \text{ day}, & n = 1 \\ 
6 \text{ days}, & n = 2 \\ 
\text{Round}(I(n - 1) \cdot EF'), & n > 2 
\end{cases}$$

---

## 3. Rating Scale & Behavioral Matrix

Users evaluate their recall using 4 discrete buttons or keyboard shortcuts during a study session:

| Rating | Quality ($q$) | Keyboard | User Experience | Repetitions ($n$) | Interval Impact | Ease Factor ($EF$) |
| :---: | :---: | :---: | :--- | :---: | :--- | :--- |
| **Again** | `1` | <kbd>1</kbd> | Failed recall; complete memory lapse | Reset to `0` | Reset to `1` day | Reduced by $-0.20$ |
| **Hard** | `2` | <kbd>2</kbd> | Correct recall with severe hesitation/effort | Incremented ($n+1$) | $I(n) = \max(1, \text{Round}(I \cdot 1.2))$ | Reduced by $-0.15$ |
| **Good** | `4` | <kbd>3</kbd> | Successful, confident recall | Incremented ($n+1$) | $I(n) = \text{Round}(I \cdot EF')$ | Minor adjustment ($-0.00$) |
| **Easy** | `5` | <kbd>4</kbd> | Instant, effortless recall | Incremented ($n+1$) | $I(n) = \text{Round}(I \cdot EF' \cdot 1.3)$ | Increased by $+0.15$ |

---

## 4. Worked Example: 5-Stage Review Progression

Consider a new word initialized with $EF = 2.50$, $I = 0$, $n = 0$:

1. **Review 1 (Rated `Good`, $q=4$)**:
   - $EF' = \max(1.3, 2.50 + (0.1 - 1 \cdot (0.08 + 0.02))) = 2.50$
   - $n = 1 \implies I = 1\text{ day}$.
2. **Review 2 (Rated `Good`, $q=4$)**:
   - $EF' = 2.50$
   - $n = 2 \implies I = 6\text{ days}$.
3. **Review 3 (Rated `Easy`, $q=5$)**:
   - $EF' = \max(1.3, 2.50 + (0.1 - 0)) = 2.60$
   - $n = 3 \implies I = \text{Round}(6 \cdot 2.60 \cdot 1.3) \approx 20\text{ days}$.
4. **Review 4 (Rated `Good`, $q=4$)**:
   - $EF' = 2.60$
   - $n = 4 \implies I = \text{Round}(20 \cdot 2.60) = 52\text{ days}$.
5. **Review 5 (Lapse: Rated `Again`, $q=1$)**:
   - $EF' = \max(1.3, 2.60 + (0.1 - 4 \cdot (0.08 + 0.08))) = \max(1.3, 2.60 - 0.54) = 2.06$
   - $n = 0$, $I = 1\text{ day}$, $\text{Lapses} = 1$, Status = `Learning`.

Notice how the algorithm penalizes forgotten knowledge immediately while retaining long-term stability once re-acquired.

---

## 5. Learning Status Lifecycle

Every word's review card progresses through well-defined lifecycle states:

```mermaid
stateDiagram-v2
    [*] --> New: Word Created
    New --> Learning: First Review Attempted
    Learning --> Reviewing: Repetitions >= 2 (Good / Easy)
    Reviewing --> Mastered: Repetitions >= 5 & Interval >= 21 Days
    Reviewing --> Learning: Failed Recall (Again)
    Mastered --> Learning: Failed Recall (Again)
```

- **`New`**: Word added to dictionary, awaiting first study session.
- **`Learning`**: Card actively undergoing early consolidation ($1 \le \text{Repetitions} < 2$).
- **`Reviewing`**: Stable retention; intervals expanding exponentially ($2 \le \text{Repetitions} < 5$).
- **`Mastered`**: High memory stability; card has survived $\ge 5$ consecutive successful reviews and interval exceeds $21$ days.
- **Lapse Handling**: Any card graded as **Again** automatically increments `Lapses`, resets `Repetitions` to `0`, schedules the next review for tomorrow, and returns the status to `Learning`.

---

## 6. Daily Streak Mechanics

- Completing at least one card review on a given calendar day advances the streak.
- If the user was active **yesterday** (in local user timezone), `CurrentStreak` increments by $+1$.
- If the user was active **today**, the streak is preserved without double-incrementing.
- If a full calendar day elapsed without activity, `CurrentStreak` resets to $1$.
- `LongestStreak` automatically updates whenever `CurrentStreak > LongestStreak`.
