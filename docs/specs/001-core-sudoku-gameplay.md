# Spec 001 — Core Sudoku Gameplay

**Status:** Ready for implementation
**Milestone:** Core (pre-meta, pre-monetization)
**Unity:** 6000.5.10f1 · URP · uGUI + TextMeshPro
**Date:** 2026-08-27

---

## Problem Statement

There is no game. The repository is a bare Unity URP template with a single empty
sample scene and no gameplay code, no content pipeline, and no version control.

The intent is a casual mobile Sudoku game in which Sudoku is the *core mechanic*
rather than the whole product — meta systems (daily challenge, currency, cosmetic
themes, achievements, seasonal events), in-app purchases, and rewarded ads are all
expected to arrive later. That future creates the actual problem this spec solves:
a core built without those seams in place has to be torn open to accept them, and
the retrofit cost lands precisely on the code that is hardest to change safely —
puzzle generation, input handling, and every colour and string in the UI.

A player, meanwhile, needs a Sudoku game that is fair, resumable, and pleasant on
a phone: puzzles whose stated difficulty matches how they actually feel, mistakes
flagged the moment they happen rather than twenty moves later, a game that survives
being backgrounded mid-puzzle, and a presentation with enough personality to be
worth returning to.

## Solution

Ship a portrait-only mobile Sudoku game built in two clearly separated halves.

The lower half is `Sudoku.Core`: a pure C# library with zero `UnityEngine`
references, containing the grid and constraint model, a counting solver, a
human-technique solver that doubles as both difficulty grader and hint engine, a
puzzle generator, an undoable command stack, and the game session state machine.
Because it has no engine dependency, it is exhaustively unit-testable, and it is
tested exhaustively — the generator is the highest-risk, lowest-visibility part of
the product, and a bug there ships unsolvable puzzles to players.

The upper half is `Sudoku.Game`: a thin uGUI presentation layer over that core.
It renders the board, runs the input state machine, drives the juice, and owns
persistence. It talks to the core through explicit interfaces, and it never
contains game rules.

Puzzles are not generated on device. An editor bake tool runs the same generator
offline to produce five difficulty tiers of 2,000 uniquely-solvable, difficulty-
graded puzzles plus a separate date-seeded daily bank, shipped as a compact
versioned binary. This gives instant load, QA-able difficulty, and a recalibration
path that is a re-bake rather than a code release.

Visually the game commits to a "chunky playful" millennial direction — fat rounded
rectangles, thick borders, hard offset shadows that collapse on press, saturated
candy colours, overshoot easing — chosen specifically because it is buildable
entirely from rounded rects and tweens and therefore requires no illustrator.
Humour lives in copy and motion on every surface *except* inside the puzzle, in a
deadpan, self-deprecating voice, sourced from a single table so it is localizable
and swappable from day one.

Every element a future meta system will need is present as an interface with a
trivial implementation: a structured gameplay event stream, an analytics service,
a consumable service fronting hearts and hints, ScriptableObject theming, and
date-seeded daily determinism.

---

## User Stories

### Playing a puzzle

1. As a player, I want to select a difficulty from five clearly named tiers, so that I can pick a puzzle matched to how much effort I want to spend.
2. As a player, I want the difficulty I picked to reliably predict how hard the puzzle feels, so that I trust the game's labelling.
3. As a player, I want every puzzle to have exactly one solution, so that I am never punished for a logically valid deduction.
4. As a player, I want to tap a cell to select it and then tap a digit to place it, so that input works the way every other mobile Sudoku does.
5. As a player, I want the selected cell to be visually obvious, so that I never place a digit in the wrong place because I lost track of my selection.
6. As a player, I want the row, column, and box of my selected cell subtly highlighted, so that I can scan constraints without tracing them by eye.
7. As a player, I want every cell containing the same digit as my selected cell highlighted, so that I can quickly find where a digit can still go.
8. As a player, I want to tap a given clue cell and still get peer highlighting, so that I can use clues as scanning anchors.
9. As a player, I want an attempt to overwrite a given clue to be visibly rejected rather than silently ignored, so that I understand why nothing happened.
10. As a player, I want a digit that is already placed nine times to grey out on the numpad, so that I get free progress feedback without counting.
11. As a player, I want each numpad digit to show how many of that digit remain unplaced, so that I can prioritise which digit to hunt for.
12. As a player, I want to erase the contents of a selected cell, so that I can clear a mistake without cycling through digits.
13. As a player, I want the board to be as large as the portrait screen allows, so that my thumb hits the cell I aimed at.
14. As a player, I want the game locked to portrait, so that the layout never breaks when I lie down.

### Mistakes and hearts

15. As a player, I want a digit that differs from the puzzle's actual solution to be flagged immediately, so that I never discover a fatal error twenty moves later.
16. As a player, I want a wrong digit to stay on the board in an error state rather than being rejected, so that I can see what I did and correct it deliberately.
17. As a player, I want errors signalled by more than colour alone, so that the game works for me if I am colourblind.
18. As a player, I want a short shake on a wrong entry, so that the failure registers physically as well as visually.
19. As a player, I want a limited number of mistakes per puzzle, so that the game has stakes and my correct solves feel earned.
20. As a player, I want to turn the mistake limit off in settings, so that I can play a relaxed, consequence-free game when I want to.
21. As a player, I want to turn immediate mistake highlighting off in settings, so that I can play a stricter, self-checked game if I prefer.
22. As a player, I want re-tapping the same wrong digit into the same cell to cost nothing, so that a fat-fingered double-tap does not end my run.
23. As a player, I want notes to never cost me a mistake, so that I can speculate freely, which is the entire point of notes.
24. As a player, I want a clear, non-punitive screen when I run out of mistakes, so that losing does not feel like the app broke.
25. As a player, I want my total mistakes shown at the end, so that I can gauge my own improvement over time.

### Notes

26. As a player, I want to toggle a notes mode, so that I can record candidate digits in a cell without committing to one.
27. As a player, I want notes rendered small and clearly distinct from placed digits, so that I never confuse a guess with an answer.
28. As a player, I want to long-press a numpad digit as a shortcut for entering it as a note, so that I can annotate quickly once I know the trick.
29. As a player, I want placing a digit to automatically remove that digit from the notes of every cell in its row, column, and box, so that I do not spend the game doing bookkeeping.
30. As a player, I want that auto-removal to be toggleable, so that I can keep manual control if I prefer.
31. As a player, I want a single undo to reverse both my placement and all the notes it auto-removed, so that undo restores the board exactly as it was.

### Undo, hints, and assistance

32. As a player, I want unlimited undo, so that experimenting never feels risky.
33. As a player, I want one tap of undo to reverse exactly one thing I did, so that undo is predictable rather than surprising.
34. As a player, I want undo to work on notes and erases, not only placements, so that it covers everything I can actually do.
35. As a player, I want undo to *not* give me back a lost heart, so that the mistake system means something.
36. As a player, I want a limited number of hints per puzzle, so that I have an escape hatch from being stuck without trivialising the puzzle.
37. As a player, I want a hint to first show me *which* cell is solvable and *why* — highlighting the row, column, or box that forces it — so that I learn a technique instead of just receiving an answer.
38. As a player, I want a second tap to actually fill the hinted cell, so that I can take the nudge without the answer if I want to.
39. As a player, I want a hint to prefer the cell I have selected when that cell is currently solvable, so that hints answer the question I am actually asking.
40. As a player, I want a hint to never be consumed on a cell I already filled correctly, so that hints are never wasted.
41. As a player, I want a hint to cost no mistake, so that asking for help is not double-punished.
42. As a player, I want my hint usage recorded and shown, so that I know whether a solve was truly unassisted.

### Timing and results

43. As a player, I want a running timer, so that I can measure myself against my past solves.
44. As a player, I want to hide the timer, so that I can play without time pressure.
45. As a player, I want the timer to pause when I pause, open settings, or leave the app, so that my recorded time reflects time actually spent playing.
46. As a player, I want a satisfying animation sweeping across the board when I complete a puzzle, so that finishing feels like a payoff rather than a state change.
47. As a player, I want a results card showing difficulty, time, mistakes, and hints used, so that I can see how the solve went at a glance.
48. As a player, I want my best time per difficulty tracked and celebrated when beaten, so that I have a reason to replay a tier.
49. As a player, I want a prominent "Next Puzzle" action on the results card, so that I can stay in the loop without navigating.
50. As a player, I want the results card to say something with personality about how I did, so that the game feels like it noticed.
51. As a player, I want that comment to vary between solves, so that it does not become wallpaper within one session.

### Continuity and persistence

52. As a player, I want my in-progress puzzle saved automatically after every move, so that I never lose progress to a crash or a phone call.
53. As a player, I want to background the app mid-puzzle and return to the exact same board, notes, timer, and hearts, so that the game fits into the gaps in my day.
54. As a player, I want my undo history preserved across a resume, so that resuming does not silently cripple undo.
55. As a player, I want one in-progress puzzle per difficulty, so that starting a quick Easy game does not destroy my half-finished Expert.
56. As a player, I want the home screen to offer "Continue" for my most recent unfinished puzzle, so that resuming is one tap.
57. As a player, I want difficulty select to show me which tiers have a game waiting, so that I can decide between continuing and starting fresh.
58. As a player, I want a warning before abandoning an in-progress puzzle, so that I do not discard work by accident.
59. As a player, I want the same puzzle never to be served to me twice, so that the content feels endless.
60. As a player, I want my settings to persist across launches, so that I configure the game once.
61. As a player, I want a future app update to not wipe my saves, so that I keep playing across versions.

### Look, feel, and accessibility

62. As a player, I want the interface to look playful and modern rather than like a spreadsheet, so that opening it is pleasant.
63. As a player, I want buttons that visibly depress when tapped, so that every interaction feels physical.
64. As a player, I want a dark theme, so that I can play at night without being blinded.
65. As a player, I want to switch themes at any time and have the change apply instantly, so that I am not made to restart.
66. As a player, I want a light haptic tap when I place a digit and a firmer one on a mistake, so that the game feels responsive in my hand.
67. As a player, I want sound effects for placement, errors, and completion, so that actions have weight.
68. As a player, I want to mute sound and haptics independently, so that I can play in a meeting.
69. As a player, I want the game to respect my device's Reduce Motion setting by damping the bouncy animations, so that playing does not make me nauseous.
70. As a player, I want digits and notes legible at a glance, so that I do not misread a 6 as an 8 at note size.
71. As a player, I want the game to respect notches and home indicators, so that no control is clipped or unreachable.
72. As a player, I want jokes to stay out of the puzzle itself, so that my concentration is never interrupted.

### Developer- and business-facing

73. As a developer, I want the game rules and generator to be pure C# with no engine dependency, so that they can be tested in seconds without opening Unity.
74. As a developer, I want a headless command that runs the full core test suite, so that I can verify correctness on every change without manual play.
75. As a developer, I want a test asserting that thousands of generated puzzles are all uniquely solvable, so that an unsolvable puzzle can never ship.
76. As a developer, I want a test asserting that generated puzzles grade within their intended tier, so that difficulty labelling stays honest.
77. As a developer, I want an editor menu command that bakes all puzzle banks and prints a validation report, so that content generation is a repeatable, auditable step.
78. As a developer, I want the bake to be deterministic from a fixed seed, so that the same bank can be reproduced later.
79. As a developer, I want difficulty thresholds stored as data rather than code, so that recalibrating difficulty is a re-bake rather than a release.
80. As a developer, I want gameplay to emit a structured event stream, so that meta systems and analytics can subscribe without gameplay knowing they exist.
81. As a developer, I want an analytics interface with a console implementation, so that the event schema is fixed and exercised before any SDK is chosen.
82. As a developer, I want hearts and hints to be spent through a single consumable interface, so that ads and IAP plug in later without touching gameplay code.
83. As a developer, I want every user-facing string in one table, so that localization and copy A/B tests are possible without touching prefabs.
84. As a developer, I want every colour and font referenced through a theme asset, so that a cosmetics shop later ships data rather than code.
85. As a developer, I want the daily puzzle derived deterministically from the date, so that every player worldwide gets the same puzzle offline.
86. As a developer, I want the save format versioned with a migration hook, so that schema changes never orphan a player's progress.
87. As a developer, I want the project under version control with a correct Unity gitignore, so that work is not one corrupted `Library` away from loss.
88. As a business stakeholder, I want abandonment tracked per difficulty, so that we can detect a miscalibrated tier before we have any other signal.
89. As a business stakeholder, I want the seam for an interstitial reserved between board completion and the results card, so that ads can be introduced later without reworking the flow.

---

## Implementation Decisions

### Assemblies and boundaries

- Four assembly definitions: **`Sudoku.Core`** (pure C#, zero `UnityEngine` references), **`Sudoku.Core.Tests`** (EditMode), **`Sudoku.Game`** (Unity runtime), **`Sudoku.Editor`** (bake tool, editor-only).
- The `Core` → `Game` direction is forbidden and enforced by the asmdef reference graph, not by convention. All game rules live in `Core`; the Unity layer is presentation and platform services only.
- Feature-based folder layout under `Assets/_Project/` — `Core/`, `Game/` (subfoldered `Board`, `Input`, `Session`, `Save`, `Theme`, `Screens`, `Audio`), `Editor/`, `Art/`, `Fonts/`, `Data/`, `Scenes/`. Root namespace `Sudoku`, mirroring folders.

### Grid and constraint model

- The board is a flat value array of size `N*N` with an injectable constraint set — a collection of cell-index groups each asserting "these cells contain distinct values" — rather than hardcoded row/column/box arithmetic.
- Only classic 9×9 ships. The generality exists so that Sudoku-X, Jigsaw, or reduced-size boards become additional constraint groups rather than a rewrite. No variant content, UI, or generation tuning is built.
- Notes are stored as a 9-bit mask per cell.

### Solving, grading, and hints — one component

- A **counting solver** (backtracking, early-exit at two solutions) answers "is this puzzle uniquely solvable". Used by generation and bake validation only.
- A **human-technique solver** applies techniques in escalating order (naked singles → hidden singles → locked candidates → naked/hidden pairs and triples → X-Wing → …), reporting at each step which technique fired, which cell it resolved, and which constraint group forced it.
- That single component serves three roles: it **grades** difficulty (by hardest technique required, weighted by how often each tier of technique is needed), it **powers hints** (the easiest next logical step, plus the reason to highlight), and it **validates** the bake. This unification is deliberate and load-bearing.
- Difficulty is graded by required technique, explicitly **not** by clue count.
- Grading cut points live in a `DifficultyProfile` ScriptableObject, hand-set initially and recalibrated later against solve-time percentiles and abandonment rates from telemetry.

### Content pipeline

- Puzzles are **baked offline**, never generated on device. An editor menu command runs the generator and emits the banks plus a validation report.
- Five tiers — Easy, Medium, Hard, Expert, Master — at **2,000 puzzles each**.
- Guarantees enforced at bake time: **unique solution (mandatory)**, **180° rotational clue symmetry for Easy/Medium/Hard** (relaxed for Expert/Master, where symmetry caps achievable difficulty), **exact-hash deduplication** across the bank, and **grade within tier**. Strict clue minimality is *not* required.
- Bank format: a versioned binary blob at 4 bits per clue cell plus 4 bits per solution cell — 81 bytes per puzzle, roughly 810 KB total — indexed by offset and read without parsing. Shipping the solution is what makes solution-based mistake detection free at runtime.
- The bake is deterministic from a fixed RNG seed and re-runnable, so a difficulty recalibration is a content change rather than a code release.
- A **separate daily bank** with a weekday difficulty curve (easier early week, hardest at the weekend). Date maps to index by hashing the ISO date and taking it modulo bank size, so the mapping is identical for every player and works offline.
- Played-puzzle indices are recorded per tier so no puzzle repeats for a player.

### Session, commands, and events

- All board mutation goes through an undoable command abstraction — apply and revert against grid state. One player action is one **composite** command: a placement plus its auto-removed peer notes undoes atomically.
- The undo stack is unlimited during play and persisted capped at 200 entries. **No redo.** Undo never refunds hearts or hints; refunding would make the mistake economy decorative and unmonetizable.
- Mistakes are **solution-based** — a digit differing from the known unique solution — not conflict-based. A wrong digit is placed and held in an error state; the player must erase or undo it. Re-placing the identical wrong digit into the same cell is a no-op and costs nothing. Notes never cost hearts.
- Lifetime mistake count is tracked independently of the heart counter, so perfect-solve statistics survive any future change to the heart system.
- Hearts default to 3 and hints to 3 per puzzle; both are toggleable/configurable and both are spent exclusively through an **`IConsumableService`**, so rewarded-ad refills and IAP bundles later require no gameplay change.
- A session state machine governs Playing / Paused / Completed / Failed, and gates the timer.
- Gameplay emits a **structured event stream** consumed by analytics and, later, by meta systems: puzzle started, cell placed, mistake made, hint used, note toggled, undo used, hearts depleted, puzzle completed, puzzle abandoned, screen viewed, setting changed. Common parameters — session, difficulty, puzzle, theme, app version — are attached centrally.
- Analytics is fronted by `IAnalyticsService` with a console implementation; no SDK is bound in this milestone. Cell-placement events are the highest-volume by an order of magnitude and are batched or sampled rather than sent individually.

### Input

- **Cell-first** only: select cell, then choose digit. The input layer is a state machine so that digit-first becomes an additional state later rather than a rewrite.
- Numpad layout: a single row of nine digits with remaining-count badges, above which sits an action row — Undo, Erase, Notes toggle, Hint (with remaining count). Digits become non-interactive at nine placements.
- Notes mode is an explicit toggle, with long-press on a digit as a secondary shortcut. No double-tap gestures — they conflict with fast tapping.
- Tapping a given clue selects it for peer highlighting but rejects edits with a shake.

### Timing

- Elapsed time accumulates unscaled frame delta while the session is in Playing. **Wall-clock is never consulted** — it makes clock changes a cheat vector and a bug source once leaderboards exist.
- The timer pauses on the pause screen, on settings overlays, on application pause/focus loss, and on results. It does not pause during hint or error animations.

### Persistence

- **One in-progress slot per difficulty (five) plus one daily slot.** Each slot serializes grid state, notes, elapsed time, hearts, hints, mistake count, the capped undo stack, and the puzzle's bank reference.
- Autosave fires on every committed move, written asynchronously off the main thread via atomic temp-file-then-rename, with a forced synchronous flush on application pause and focus loss — mobile processes are killed without warning.
- JSON via Unity's built-in serializer into the platform persistent data path. DTOs are array-based to avoid a Newtonsoft dependency. Every payload carries a `schemaVersion` and passes through a migration hook from the first release.

### Presentation, theme, and copy

- **uGUI + TextMeshPro.** The board is 81 pooled cell views with fixed anchors baked once from a grid layout. The board view sits behind an interface so a single-mesh implementation remains possible if profiling ever demands it.
- A **`ThemeDefinition` ScriptableObject** holds the full palette — backgrounds, board lines, cell states (default, selected, peer, same-digit, error), text colours for given/entered/error/note, numpad states — **and font references**. A theme service applies it at runtime to subscribing themed components; switching is instant. Light and Dark ship.
- Visual direction is "chunky playful": ~24px corner radii, 4px borders, a 6px hard bottom shadow collapsing to 2px on press, overshoot easing throughout, saturated candy tones over an off-white ground. Built entirely from rounded rectangles and tweens — **no illustration assets required**.
- Type: Fredoka (SemiBold/Bold) for headings, buttons, and numpad; Nunito (Bold/ExtraBold) for board digits and notes, chosen for unambiguous numerals at note size. Both OFL-licensed, baked as SDF16 static ASCII atlases.
- **All user-facing strings live in a single copy table**, never in prefabs — this is what makes localization and copy experiments possible later. Voice is deadpan, self-deprecating, observational; never exclamatory.
- Humour appears on home, results, pause, settings, and achievement naming — **never inside the puzzle**. In-puzzle personality is motion only. Results-card reactions are drawn from pools of 5–10 variants per outcome bucket (fast, slow, perfect, mistake-heavy, hint-heavy) so lines do not repeat within a session.
- Errors carry a **redundant non-colour signal** — a shake on entry plus a persistent underline — so colour is never the sole carrier of meaning. Reduce Motion is honoured by damping overshoot easing.

### Audio and haptics

- An `IAudioService` over an AudioMixer with separate SFX and music groups and independent mutes. Approximately eight CC0 effects: place, erase, error, hint, box complete, puzzle complete, button tap, heart lost. **No music** — most Sudoku players mute it.
- Haptics on the same service: light impact on placement, heavier on error. The iOS silent switch is respected.
- Sound and haptics default on at first launch.

### Screens and flow

- Six screens: Home (Continue / New Game / Daily, disabled), Difficulty Select, Game, Results, Pause, Settings.
- Completion flow: board cascade animation → **[reserved interstitial seam]** → results card → primary "Next Puzzle" (same difficulty), secondary "Home". The ad seam exists in the state machine from the start even though nothing occupies it.

### Dependencies

- **PrimeTween** for tweening — zero-allocation matters in a game that tweens on every tap.
- TextMeshPro via the existing uGUI package.
- **No Addressables**, **no DI framework**, **no Newtonsoft**, **no Unity Localization package**, **no analytics SDK** in this milestone. A hand-rolled composition root wires the object graph. Each of these is adopted when a concrete need arrives, not before.
- The URP 3D template content (sample scene contents, tutorial readme, PC renderer) is stripped; mobile renderer retained.

### Project hygiene

- `git init` with a Unity `.gitignore` (`Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, `.idea/`), forced text serialization, visible meta files, and a `.gitattributes` pre-configured for Git LFS on `*.png`, `*.ttf`, `*.wav`. **Git LFS is not currently installed on this machine**; the attributes file is staged for when it is.
- No remote is configured or pushed to.

---

## Testing Decisions

### What makes a good test here

Tests assert **external behaviour at the highest available seam** — the shape of the board after a sequence of player actions, the properties of a generated puzzle, the state of a session after a resume — never internal structure, private helpers, or the order in which techniques were tried. A test that would fail on a refactor that preserves behaviour is a bad test.

The overwhelming majority of value sits in `Sudoku.Core`, because it is pure, deterministic, fast, and carries all the risk. The Unity layer gets very few tests; it is verified by playing it.

### Seams

The ideal is one seam, and this design comes close. There are three, in descending order of coverage:

1. **`GameSession` — the primary seam.** A session is constructed from a puzzle definition and a rules configuration, driven by player-intent methods (select a cell, place a digit, toggle a note, erase, undo, request a hint, tick time, pause), and observed through its exposed board state, counters, status, and emitted event stream. Virtually every behavioural rule in this spec — mistakes, hearts, notes auto-removal, composite undo, hint consumption, win and loss detection, timer gating, event emission — is testable through this one seam with no engine, no mocks, and no scene. **This is where the bulk of the suite lives.**

2. **`PuzzleGenerator` / `PuzzleGrader` — the property seam.** Generation is non-deterministic in output but strongly propertied, so it is tested as invariants over volume rather than as fixed expectations: across thousands of generated puzzles, every one is uniquely solvable, every one grades within its requested tier, symmetry holds where required, and generation terminates within a bounded attempt count. A fixed seed makes any failure reproducible.

3. **`SaveSerializer` — the round-trip seam.** A session is mutated through an arbitrary action sequence, serialized, deserialized, and asserted equivalent — board, notes, timer, counters, and undo stack included. Schema migration is tested by deserializing checked-in fixture payloads from prior versions.

The bake tool is *not* a fourth seam: it is a thin editor wrapper around seam 2, and its correctness is the validation report it prints.

The Unity layer deliberately introduces **no new test seams**. Adding a PlayMode harness for board rendering or the input state machine would cost more than it protects, given that all rules live below it.

> **These seams need your confirmation before implementation begins** — particularly that `GameSession` is a rich enough surface to carry the bulk of the suite, rather than tests reaching into the grid model directly.

### Modules under test

| Module | Seam | Emphasis |
|---|---|---|
| Session, rules, mistakes, hearts | `GameSession` | Exhaustive — this is the behavioural contract |
| Notes and auto-removal | `GameSession` | Exhaustive, including composite-undo atomicity |
| Undo / command stack | `GameSession` | Exhaustive, including no-refund and cap behaviour |
| Hint engine | `GameSession` | Selection preference, never-wasted, consumption accounting |
| Technique solver / grader | `PuzzleGrader` | Table-driven against hand-authored puzzles of known grade |
| Counting solver | direct | Known-unique, known-multiple, known-unsolvable fixtures |
| Generator | property tests | Volume invariants under fixed seed |
| Save / migration | `SaveSerializer` | Round-trip equivalence, versioned fixtures |
| Constraint model | via the above | No direct tests — it has no behaviour of its own |

### Prior art

None — this is the first code in the repository. The EditMode conventions established by this spec (fixed seeds, fixture puzzles as string constants, property tests over volume, one assertion subject per test) become the prior art that later specs follow. `com.unity.test-framework` is already a project dependency; `Sudoku.Core.Tests` is created against it, and the suite is run headlessly from the installed editor CLI as the gate on every core change.

---

## Out of Scope

**Meta systems.** Daily challenge UI (calendar, streak display) — the bank and date-seeding are built, the screen is not; the Home entry point is present and disabled. Also out: soft currency, shop, cosmetic theme packs beyond Light/Dark, achievements, seasonal events, battle pass, leaderboards, friends, async multiplayer.

**Monetization.** No IAP, no ad SDK, no rewarded video, no interstitials. Only the interfaces and the one flow seam they will later occupy.

**Sudoku variants.** Killer, Sudoku-X, Jigsaw, Samurai, and reduced-size boards. The constraint model admits them; nothing is built for them.

**Alternate input.** Digit-first entry, drag-and-drop, double-tap gestures. The state machine admits digit-first later.

**Auto-notes** (filling all legal candidates on demand), redo, and puzzle sharing or import.

**Platform breadth.** Landscape layouts, tablet-specific layouts, WebGL, desktop, and the URP PC renderer.

**Localization.** Strings are table-driven and ready; only English exists and the Unity Localization package is not installed.

**Onboarding.** No tutorial — the audience knows Sudoku. A single tooltip on the Notes button covers the one non-obvious control.

**Accounts, cloud save, and cross-device sync.** Local storage only.

**Music.** SFX and haptics only.

**Star ratings and scoring.** Time and best-time only; a numeric score is deferred until leaderboards create a need.

**Server-delivered content**, remote config, and A/B infrastructure.

---

## Further Notes

### Build order

Each step ends in something verifiable, with three review checkpoints:

1. Repo and project hygiene — git, gitignore, folder structure, asmdefs, template cleanup, PrimeTween and fonts.
2. `Sudoku.Core`, test-first — model, counting solver, technique solver/grader, generator, command stack, session state machine.
3. Bake tool — five tier banks plus daily bank, with validation report. **→ Checkpoint 1: tests green, banks on disk.**
4. Playable greybox — board, input, mistakes, notes, undo, hints, timer, win detection. Deliberately ugly, functionally complete. **→ Checkpoint 2: a puzzle is solvable end-to-end on device.**
5. Save/resume and settings.
6. Screens and flow wiring.
7. Skin, juice, audio, copy voice. **→ Checkpoint 3: it looks like the product.**
8. Analytics interface and console implementation; device build.

Steps 2 and 3 write **zero `UnityEngine` code** — deliberately. The riskiest part of the project is also the cheapest to test exhaustively, so it is finished and proven before any pixel exists. Step 4 being ugly is the point: if it is fun in greybox, the skin makes it good; if it is not, no skin saves it.

### Definition of done

A portrait build running on a physical device where a player can pick a difficulty, solve a puzzle end-to-end using notes, undo, hints, and hearts, background the app mid-puzzle and resume exactly where they left off, see a themed and animated results card, and start the next puzzle — alongside a green headless EditMode suite proving the generator never emits an invalid or mis-graded puzzle.

### Glossary

Established here for use in all subsequent specs and code.

- **Cell** — one of the 81 positions. **Given** / **clue** — a cell pre-filled by the puzzle, never editable. **Entry** — a digit placed by the player. **Note** / **candidate** — a small speculative digit; multiple per cell.
- **Peer** — a cell sharing a row, column, or box with another. **Constraint group** — a set of cells required to hold distinct values.
- **Puzzle** — clues plus their unique solution. **Bank** — a baked, tiered collection of puzzles. **Tier** — one of the five difficulty levels.
- **Technique** — a named human deduction method. **Grade** — the tier assigned by the technique solver.
- **Heart** — one unit of the per-puzzle mistake allowance. **Mistake** — an entry differing from the solution.
- **Session** — one play-through of one puzzle. **Slot** — persisted storage for one in-progress session.
- **Command** — one undoable player action, possibly composite.

### Known risks

- **Difficulty calibration will be wrong on the first attempt.** This is expected and is why thresholds are data and the bake is re-runnable. `puzzle_abandoned` per tier is the earliest corrective signal available.
- **Generation cost for the top tiers.** Master-grade puzzles with relaxed symmetry may need many generate-and-discard cycles. This is an offline bake cost, so it is a build-time inconvenience rather than a player-facing risk — but a generous attempt budget and a progress report in the bake tool are warranted.
- **The humour is the hardest thing to get right and the easiest to get wrong.** Deadpan lands or grates with little middle ground, and it is the one element here that cannot be validated by a test. Keeping it entirely outside the puzzle limits the blast radius; keeping it in one table makes a full-voice rewrite cheap.
- **Two fonts plus a bouncy motion language is a real polish burden** for a solo effort. If step 7 threatens the schedule, the correct cut is motion complexity, not the theme system — the theme system is what future cosmetics revenue depends on.

---

## Addendum: deviations recorded during implementation

These were decided while building step 1-3 and are recorded here so the spec
stays the authoritative description of what exists.

1. **A standalone test/bake runner under `tools/`.** Unity ships a full .NET 8
   SDK inside the editor install. Because `Sudoku.Core` is engine-free, the same
   source files are also compiled by three small `dotnet` projects: a test
   runner (`tools/test.sh`), the bank baker (`tools/bake.sh`), and the bank
   verifier (`tools/verify-banks.sh`). This buys a ~5ms test loop instead of
   ~60s of Unity batchmode, works while the editor is open, allows CI to rebuild
   banks with no Unity licence, and mechanically enforces Core's zero-
   `UnityEngine`-references rule. These are additional runners over the same
   code, not a fork: the tests are ordinary EditMode tests and still run in the
   Unity Test Runner, and the editor bake window calls identical Core code.

2. **A hand-rolled PRNG instead of `System.Random`.** The spec requires a bake
   to be reproducible from its seed. `System.Random` makes no cross-runtime
   guarantee across .NET, Mono and IL2CPP, so `DeterministicRandom` owns the
   algorithm.

3. **Grade-aware carving rather than rejection sampling.** Carving greedily for
   uniqueness produces an Easy puzzle roughly once in sixty attempts, which
   makes rejection sampling unusable at the easy end. Clues are therefore
   removed only while the puzzle stays at or below the target tier.

4. **Bank format is one byte per cell, not two nibbles per cell.** The spec
   estimated 4 bits of clue plus 4 bits of solution packed into 81 bytes per
   puzzle. That is what shipped - one byte holds both nibbles - so the size
   estimate stands at 81 bytes per puzzle.

5. **Daily banks are per-tier.** The spec described a single daily bank with a
   weekday difficulty curve. Since the weekday selects the tier and the date
   hash selects the puzzle, the tier must be chosen before indexing - so the
   daily set is five banks, one per tier, kept separate from the main
   progression banks.

6. **`Sudoku.Game` references `Sudoku.Core`, `UnityEngine.UI`,
   `Unity.TextMeshPro`, `Unity.InputSystem` and `Unity.InputSystem.ForUI`.**
   PrimeTween is installed but still unreferenced, because an unresolved
   assembly-definition reference breaks the whole Unity compile; it is added
   when code actually uses it, in the motion step. *(Corrected during step 5:
   this item previously said the Input System was also unreferenced. It has
   been referenced since the greybox input loop landed.)*

7. **The bundle identifier is `com.hladunoleksandr.sudoku`** on Android and
   iOS. It is permanent once published. *(Corrected during step 5: this item
   previously described the identifier as the placeholder
   `com.changeme.sudoku`, which was resolved before this branch began. The
   Standalone identifier is still the URP template default, which is harmless
   while desktop stays out of scope.)*

8. **The greybox UI is built in code, not authored as prefabs**, and installs
   itself via `RuntimeInitializeOnLoadMethod` so no scene has to be edited. For
   this step that is a benefit: the layout is diffable and reviewable with no
   binary scene merges. The skin pass revisits it once the visual language is
   settled.

9. **Greybox text uses legacy `UI.Text` with the engine's built-in font.**
   TextMeshPro needs its essential resources imported and real font assets
   generated from Fredoka and Nunito - both editor operations - so TMP arrives
   with the skin pass. Text only has to be legible at this stage.

10. **Banks live under `Assets/_Project/Resources/Banks/`.** Unity requires the
    literal folder name `Resources` for runtime loading, so the bake output path
    moved there from `Data/`.

11. **A compile check for the Unity layer** (`tools/check-game.sh`) builds
    `Assets/_Project/Game` against Unity's own managed assemblies in about a
    second. It cannot run anything - there is no engine - but it catches type
    and API errors without an editor round-trip.

12. **The test sources are compile-checked against Unity's own NUnit.** The
    fast runner uses a NuGet NUnit that is newer than the one Unity ships
    (`com.unity.ext.nunit` 2.1.0), and the two diverged silently: `Is.AnyOf`
    compiled and passed in `tools/test.sh` while failing to compile in the
    editor - and because Unity blocks Play mode on any compile error, including
    one in a test assembly, that stopped the game running entirely.
    `tools/test.sh` now builds the same test sources against Unity's actual
    `nunit.framework.dll` before running anything, so that class of divergence
    fails in the normal loop. `tools/check.sh` runs everything that can be
    verified without opening the editor.

13. **The two-tap hint's pending state lives in `GameSession`, not the view.**
    `PeekHint`/`UseHint` each re-derive the deduction, so two taps against a
    board that moved in between could show one cell and fill another - and
    could spend a hint on a cell the player had already filled. `GameSession`
    now holds the revealed-but-untaken hint (`PendingHint`) and exposes
    `RevealHint`, `TakeHint` and `CancelHint`; any board mutation drops it.
    "The cell you were shown is the cell that gets filled" is therefore a rule,
    tested at the `GameSession` seam with no engine, rather than a convention
    the presenter has to keep. `PeekHint`/`UseHint` remain for callers that
    want the one-shot form.

14. **Starting a puzzle over is `GameSession.Restart()`, and pausing is the
    navigator.** Restart could have been a fresh session dealt over the same
    `Puzzle` by the pause screen, but only the session knows what "back to the
    beginning" means for the board, the notes, the undo history, the clock and
    every counter it owns - and a run that ended out of hearts has to become
    playable again, which is a status change no caller can make. It is
    therefore a rule in Core, tested at the `GameSession` seam with no engine.
    Pause goes the other way: the pause screen is a screen on the back stack
    rather than a panel inside the game screen, so showing it hides the game
    screen and the existing `OnShow`/`OnHide` suspension is what stops the
    clock and takes the board out of reach. There is no second pause mechanism
    to keep in step with the first. Leaving for Home uses a new
    `Navigator.ResetTo<TScreen>()` - the player asked for Home, not for two
    steps backwards through a pause screen over a puzzle they have left.
    Restart is the only action here that destroys work, so it is the only one
    that asks twice (user story 58); leaving for Home discards nothing, so it
    does not ask at all.

15. **Preferences are declarations on one settings service, stored in
    `PlayerPrefs`.** `GameSettings` owns a `Preference<T>` per setting - read,
    written, and observed through the preference itself - and republishes every
    change on one `Changed` stream so analytics needs no per-preference wiring.
    Values round-trip through invariant text, so a new preference (the theme
    choice) is one declaration rather than a new storage case. They live in
    `PlayerPrefs` rather than the save file deliberately: a corrupt or migrated
    save must not cost the player their settings. Settings-as-an-overlay is a
    `Navigator` push rather than a second layering mechanism - the puzzle stays
    on the back stack, and `GamePresenter.OnHide` already suspends its clock.

16. **Save payloads are JSON, but encoded in `Sudoku.Core` rather than by
    `UnityEngine.JsonUtility`.** The spec named `SaveSerializer` as the
    round-trip seam and Unity's built-in serializer as the encoder, and those
    two pull in opposite directions: Core has `noEngineReferences`, so a
    serializer built on `JsonUtility` could not live at the seam the tests have
    to reach. `Core/Persistence/JsonValue.cs` is a small reader/writer covering
    exactly the payload's grammar; the DTO shape is still array-based, so the
    file remains ordinary JSON. `Sudoku.Game` keeps only what genuinely needs
    the engine: the persistent data path, the atomic write, the background
    thread, and the pause/focus flush.

17. **The save schema ships at version 2, not 1.** Version 1 is the greybox
    shape - one in-progress puzzle under `slot`, with played-puzzle counts left
    in `PlayerPrefs`. Version 2 gives every difficulty its own slot plus a daily
    one and absorbs that tracking. Keeping version 1 as a real, checked-in
    fixture (`Core.Tests/Fixtures/SavePayloads.cs`) means the migration hook is
    exercised by a payload the current serializer cannot produce, which is the
    only way the hook is worth anything.

18. **`IConsumableService` holds the hearts and hints rather than sitting beside
    them.** The spec asks that both be spent exclusively through the interface.
    An interface that only *offers* a spend method leaves the counters where
    they were, and gameplay can still decrement them - which makes the seam
    decorative exactly when it matters. `GameSession` therefore keeps no counter
    of its own: `HeartsRemaining` and `HintsRemaining` read straight off the
    service, a mistake asks it to spend a heart, a taken hint asks it to spend a
    hint before the board moves, and dealing, restarting and restoring go
    through `Reset`. Handing the session a service that refuses every spend
    leaves the hearts untouched and the run alive, which is how
    `Core.Tests/Session/ConsumableSeamTests.cs` proves nothing routes around it.
    The interface lives in Core because what a heart costs is a rule. The
    shipping implementation, `LocalConsumables`, refuses every refill and says
    so through `CanRefill`, so the out-of-hearts screen can present the offer
    and disable it rather than hide it.

19. **The completion flow is an object with an empty stage in it.**
    `Game/Session/CompletionFlow.cs` runs three named stages - board cascade,
    interstitial, results card - each taking a continuation, because the two
    still empty are both asynchronous. A null stage is skipped, so today
    completion reaches the results card immediately while the seam is a real,
    named, findable place rather than a comment. Ticket #10 fills the first
    stage; the second is reserved and deliberately never assigned. Heart
    depletion does not go through the flow: there is no cascade to play, and an
    ad after a loss is the one place an interstitial should not be.

20. **The save schema is at version 3.** Version 3 adds the best time per
    difficulty under `best`. Records go in the save file rather than
    `PlayerPrefs` so that a record and the puzzles that produced it are backed
    up, moved and cleared together. `SavePayloads.SchemaVersionTwo` is checked
    in alongside the version-1 fixture so the new migration step is exercised by
    a payload predating records rather than by one the current serializer wrote.
