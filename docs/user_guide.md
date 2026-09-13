# 📖 Lexora User Guide & Learning Manual

Welcome to **Lexora**, an intelligent desktop English learning platform engineered for Windows 11. This guide covers everything from initial setup and daily spaced repetition reviews to grammar drills, custom test simulations, and secure database backups.

---

## 📑 Table of Contents

- [1. Getting Started](#1-getting-started)
- [2. Navigation & Interface Tour](#2-navigation--interface-tour)
- [3. Managing Vocabulary](#3-managing-vocabulary)
- [4. Spaced Repetition (SM-2) Flashcards](#4-spaced-repetition-sm-2-flashcards)
- [5. Practice & Quiz Simulator](#5-practice--quiz-simulator)
- [6. Grammar Explorer & Drills](#6-grammar-explorer--drills)
- [7. Analytics, Streaks & Heatmap](#7-analytics-streaks--heatmap)
- [8. Data Portability (Import, Export & Backup)](#8-data-portability-import-export--backup)
- [9. Keyboard Shortcuts Cheat Sheet](#9-keyboard-shortcuts-cheat-sheet)

---

## 1. Getting Started

### 1.1. Installation
1. Download `Lexora-Setup-v1.0.0.exe` from the latest release.
2. Double-click the installer.
3. Follow the wizard steps to choose the install location (Default: `%LocalAppData%\Programs\Lexora` for per-user install or `C:\Program Files\Lexora` for administrator install).
4. Launch Lexora from your Start Menu or Desktop shortcut.

### 1.2. First Launch & Initial Seed Data
On your first run, Lexora automatically initializes the SQLite database at:
```text
%LocalAppData%\Lexora\data\lexora.db
```
The application comes pre-loaded with curated starter vocabulary and common English grammar structures categorized by CEFR level and exam targets (TOEIC, VSTEP, IELTS).

---

## 2. Navigation & Interface Tour

Lexora features Windows 11 Fluent Design with Mica backdrop and responsive controls:

```text
┌────────────────────────────────────────────────────────────────────────┐
│  [✦] Lexora  |  [ Dashboard ] [ Vocabulary ] [ Review ] [ Test ] [ Grammar ]  │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│   Active Content Area (Responsive UniformGridLayout / Flashcards)       │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

- **Dashboard** (<kbd>Ctrl</kbd> + <kbd>1</kbd>): High-level overview of daily cards due, active streak, review heatmap, and level distribution.
- **Vocabulary** (<kbd>Ctrl</kbd> + <kbd>2</kbd>): Full dictionary management, search, multi-faceted filtering, and word editor.
- **Spaced Review** (<kbd>Ctrl</kbd> + <kbd>3</kbd>): Active recall flashcard session powered by SuperMemo SM-2.
- **Practice & Test** (<kbd>Ctrl</kbd> + <kbd>4</kbd>): Interactive quizzes (4 test formats) with instant scoring.
- **Grammar** (<kbd>Ctrl</kbd> + <kbd>5</kbd>): Structural formulas, rule explanations, and common mistake warnings.
- **Settings**: Theme selection (Light/Dark/System Default), language toggle (English/Vietnamese), and backup tools.

---

## 3. Managing Vocabulary

### 3.1. Adding a New Word (<kbd>Ctrl</kbd> + <kbd>N</kbd>)
1. Navigate to **Vocabulary** and click **+ Add Word** or press <kbd>Ctrl</kbd> + <kbd>N</kbd>.
2. Fill in the primary details:
   - **Term**: Word or phrasal verb (e.g., *Resilience*).
   - **Phonetics**: UK IPA (e.g., `/rɪˈzɪl.jəns/`) and US IPA (e.g., `/rɪˈzɪl.jəns/`).
   - **CEFR Level**: Select from `A1`, `A2`, `B1`, `B2`, `C1`, or `C2`.
   - **Primary Part of Speech**: `Noun`, `Verb`, `Adjective`, etc.
3. Add one or more **Meanings**:
   - **Definition (Vietnamese)**: Primary translation (e.g., *khả năng phục hồi, tính kiên cường*).
   - **Definition (English)**: Clear English explanation.
   - **Context Examples**: Add bilingual sentences (e.g., *"Her resilience helped her overcome the crisis."* / *"Sự kiên cường giúp cô vượt qua khủng hoảng."*).
4. Assign **Topics** (e.g., *Psychology*, *Business*) and **Exam Tags** (e.g., *IELTS 7.0+*, *TOEIC 850+*).
5. Click **Save**. A new `ReviewCard` is automatically generated for spaced repetition!

### 3.2. Real-Time Search & Multi-Faceted Filters
- **Search Bar** (<kbd>Ctrl</kbd> + <kbd>F</kbd>): Type any term or Vietnamese translation; searches execute automatically with 250ms debouncing.
- **Level Filter**: Filter cards by CEFR level (`A1` to `C2`).
- **Exam Filter**: Filter by target examination (`TOEIC`, `VSTEP`, `IELTS`).

---

## 4. Spaced Repetition (SM-2) Flashcards

### 4.1. How Spaced Repetition Works
Lexora tracks memory decay using Piotr Woźniak's **SuperMemo SM-2** algorithm. Cards are scheduled for review right before you are predicted to forget them.

### 4.2. Review Session Flow
1. Press <kbd>Ctrl</kbd> + <kbd>3</kbd> to open the **Review** tab.
2. The card displays the English term, phonetic notation, and an optional audio playback button.
3. Attempt active recall in your mind.
4. Press <kbd>Space</kbd> or click **Flip Card** to reveal the definitions, context sentences, and nuances.
5. Grade your recall using the 4 rating buttons:

| Rating | Shortcut | When to Choose |
| :---: | :---: | :--- |
| **Again** | <kbd>1</kbd> | Complete lapse; you forgot the meaning. Card is reset to 1-day interval. |
| **Hard** | <kbd>2</kbd> | Recalled correctly but with substantial hesitation. Interval increases slightly ($\times 1.2$). |
| **Good** | <kbd>3</kbd> | Confident recall with reasonable effort. Interval expands normally by the card's Ease Factor. |
| **Easy** | <kbd>4</kbd> | Instantaneous recall. Interval receives an additional bonus multiplier ($\times 1.3$). |

> [!TIP]
> Be honest with your self-assessment! Grading a forgotten card as **Again** ensures it returns tomorrow for reinforcement before long-term memory solidifies.

---

## 5. Practice & Quiz Simulator

Lexora includes 4 interactive testing formats accessible via **Practice & Test** (<kbd>Ctrl</kbd> + <kbd>4</kbd>):

1. **Word-to-Meaning Quiz**: Look at the English term and pick the correct Vietnamese definition among 4 choices.
2. **Meaning-to-Word Quiz**: Look at the Vietnamese definition and select the target English vocabulary word.
3. **Fill-in-the-Blank**: A contextual example sentence with the target word omitted; select or type the missing word.
4. **Multiple Choice / Mixed Mode**: A randomized blend of all formats simulating official exam test conditions.

### Test Configuration:
- Select question volume: `5`, `10`, `20`, or `30` questions.
- Target specific exam domains: `TOEIC`, `VSTEP`, `IELTS`, or all vocabularies.
- **Weak Word Mode**: Focus exclusively on cards with lower Ease Factors or recent lapses.

---

## 6. Grammar Explorer & Drills

1. Press <kbd>Ctrl</kbd> + <kbd>5</kbd> to open the **Grammar** library.
2. Explore categorized rules (Tenses, Conditionals, Inversion, Relative Clauses, Collocations).
3. Each rule entry highlights:
   - **Formula Syntax**: Clear structural formula (e.g., `S + had + V3/ed + before + S + V2/ed`).
   - **Bilingual Explanations**: In-depth explanations in Vietnamese and English.
   - **Common Mistakes Box**: Prominently marked common pitfalls to avoid during writing/speaking.
   - **Interactive Practice**: Press <kbd>Ctrl</kbd> + <kbd>P</kbd> to test your understanding with sentence drills.

---

## 7. Analytics, Streaks & Heatmap

Access your **Dashboard** (<kbd>Ctrl</kbd> + <kbd>1</kbd>) to view:
- **Streak Counter**: Current consecutive daily study streak and longest streak record.
- **Daily Heatmap**: Visual contribution grid tracking review frequency across the past 365 days.
- **CEFR Mastery Breakdown**: Distribution chart showing words across A1 through C2 mastery tiers.
- **Exam Readiness Meters**: Progress bars tracking vocabulary coverage for TOEIC (990 target), VSTEP (B1-C1 targets), and IELTS (band scores).

---

## 8. Data Portability (Import, Export & Backup)

### 8.1. CSV & JSON Import / Export
- Go to **Settings** > **Data Management**.
- **Export**: Export your dictionary to RFC-4180 multiline CSV or structured JSON.
- **Import**: Select any valid CSV or JSON file (up to 20MB). Lexora validates encoding, deduplicates existing terms, and imports new vocabulary safely.

### 8.2. Database Backup & Restore
- **Create Backup**: Click **Create Backup** to generate a timestamped `.bak` file in your chosen directory.
- **Restore Backup**: Click **Restore Backup** and select your `.bak` file.
- Lexora automatically verifies the 16-byte `SQLite format 3\0` magic header, flushes connection pools, safely backs up current state, and executes an atomic replacement.

---

## 9. Keyboard Shortcuts Cheat Sheet

| Shortcut | Action | Scope |
| :--- | :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>1</kbd> | Navigate to **Dashboard** | Global |
| <kbd>Ctrl</kbd> + <kbd>2</kbd> | Navigate to **Vocabulary** | Global |
| <kbd>Ctrl</kbd> + <kbd>3</kbd> | Navigate to **Spaced Review** | Global |
| <kbd>Ctrl</kbd> + <kbd>4</kbd> | Navigate to **Practice & Test** | Global |
| <kbd>Ctrl</kbd> + <kbd>5</kbd> | Navigate to **Grammar** | Global |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | Focus Search Input | Global |
| <kbd>Ctrl</kbd> + <kbd>N</kbd> | Add New Word / Grammar Rule | Vocabulary & Grammar Pages |
| <kbd>Ctrl</kbd> + <kbd>E</kbd> | Edit Selected Item | Vocabulary & Grammar Pages |
| <kbd>Delete</kbd> | Delete Selected Item | Vocabulary & Grammar Pages |
| <kbd>Space</kbd> | Flip Flashcard (Reveal Answer) | Review Session |
| <kbd>1</kbd> | Rate Card **Again** | Review Session |
| <kbd>2</kbd> | Rate Card **Hard** | Review Session |
| <kbd>3</kbd> | Rate Card **Good** | Review Session |
| <kbd>4</kbd> | Rate Card **Easy** | Review Session |
| <kbd>Ctrl</kbd> + <kbd>P</kbd> | Launch Grammar Practice Mode | Grammar Page |
