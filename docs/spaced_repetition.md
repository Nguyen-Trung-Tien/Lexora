# Spaced Repetition System (SM-2 Algorithm)

## 1. Concept

Lexora implements an enhanced version of the **SuperMemo SM-2** spaced repetition algorithm to optimize vocabulary retention and minimize forgetting curves.

---

## 2. Review Ratings

Users evaluate their recall on a 4-point grade scale:

| Rating | Name | Description | Interval Effect | Ease Factor Adjustment |
|---|---|---|---|---|
| **1** | **Again** | Completely forgot; failed recall | Resets repetitions to 0, interval = 1 day | EF drops by 0.20 (min 1.3) |
| **2** | **Hard** | Recalled with hesitation/effort | Increments repetitions; interval = 1.2x | EF drops by 0.15 (min 1.3) |
| **3** | **Good** | Successful, standard recall | Increments repetitions; interval = interval * EF | EF remains stable |
| **4** | **Easy** | Immediate, effortless recall | Increments repetitions; interval = interval * EF * 1.3 | EF increases by 0.15 (max 3.0) |

---

## 3. Status Progression

```text
[New]
  │
  ├──► [Learning] (Initial review stage)
  │        ▲
  │        │ (After 1-4 successful Good/Easy reviews)
  ▼        │
[Remembered] ◄───┘
  │        ▲
  │        │ (Failed review resets to NotRemembered)
  ▼        │
[NotRemembered]
  │
  ▼ (After 5+ consistent successful reviews)
[Mastered]
```
