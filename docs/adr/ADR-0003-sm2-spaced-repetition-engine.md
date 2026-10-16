# ADR-0003: Spaced Repetition Scheduling Engine (SM-2 & FSRS v5)

- **Status**: Accepted
- **Date**: 2026-03-01
- **Authors**: Architectural Board
- **Deciders**: Lexora Core Team

## Context & Problem Statement
Spaced repetition is the central pedagogical mechanism in Lexora. To maximize long-term retention of exam vocabulary, the scheduling engine must calculate optimal review intervals based on user recall ratings, avoid the "ease hell" pitfall, and log study records atomically.

## Decision Drivers
- Proven mathematical foundation.
- Resilience against erratic scoring.
- Backward compatibility and seamless migration path to modern algorithms (FSRS).

## Considered Options
1. Fixed interval Leitner box system.
2. SuperMemo-2 (SM-2) algorithm.
3. Free Spaced Repetition Scheduler (FSRS v5).

## Decision Outcome
Chosen option: **Dual Engine Architecture**: Default **SuperMemo-2 (SM-2)** with an Ease Factor floor ($EF \ge 1.3$) and seamless upgrade capability to **FSRS v5**.

### Mathematical Formula (SM-2)
$$EF' = \max\left(1.3, \; EF + (0.1 - (4 - q) \times (0.08 + (4 - q) \times 0.02))\right)$$

Where $q \in \{0, 1, 2, 3, 4\}$:
- $q < 2$ (Lapse): Interval = 1 day, Repetitions = 0.
- $q \ge 2$:
  - Repetition 1: 1 day
  - Repetition 2: 6 days
  - Repetition $n > 2$: $\lceil I_{n-1} \times EF \rceil$ days.

### Positive Consequences
- Verifiable with pure unit tests (zero side effects in calculation).
- Mathematical guarantee that ease factors never decay below 1.3.
- Study sessions create immutable `StudyLog` entries for analytics.
