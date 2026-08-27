# Spec 002 — Proving It On A Device

**Status:** Draft — needs review before implementation
**Milestone:** Core, completion (still pre-meta, pre-monetization)
**Unity:** 6000.5.10f1 · URP · uGUI + TextMeshPro
**Date:** 2026-08-27
**Follows:** [`001-core-sudoku-gameplay.md`](001-core-sudoku-gameplay.md)

---

## Problem Statement

Spec 001 is merged. Five tiers of baked puzzles, a session state machine, notes,
undo, hints, hearts, save/resume, six screens, a theme system, a skin, motion,
audio, analytics and a copy voice — 277 tests green, both Unity assemblies
compiling clean.

None of it has ever been seen.

Every one of those 277 tests is `Sudoku.Core`. `Sudoku.Game` is compile-checked
and never executed; no part of the branch was run in the editor, on a
simulator, or on a phone. The skin, the motion, the safe area and every screen
layout were verified by compile-check and arithmetic on paper. Spec 001's
definition of done — *"a portrait build running on a physical device where a
player can pick a difficulty, solve a puzzle end-to-end … and start the next
one"* — is the one thing it did not deliver.

That gap is not merely "the last ticket didn't finish". It has already produced
bugs, and the evidence is specific.

Spec 001 decided, deliberately and with a stated rationale, that the Unity layer
would introduce **no new test seams**: *"Adding a PlayMode harness for board
rendering or the input state machine would cost more than it protects, given
that all rules live below it."* That reasoning held exactly as far as it
claimed. No rule broke. What broke was the **wiring between the rules and the
presentation**, which is neither, and which nothing tests:

- **"Highlight mistakes" only silenced the colour.** The shake, the error sound,
  the firm haptic, the live mistake counter, and the numpad's remaining-count
  badge all still announced every mistake the instant it happened — and the
  badge refusing to decrement actively leaked *which* digit was wrong. Five
  channels, four ignoring the preference, because each arrived with a different
  ticket that knew only its own surface.
- **`puzzle_abandoned` was emitted into a session nobody was listening to**, and
  when it was heard, it was filed under whatever tier was last played. The spec
  names that event as the earliest available signal of a miscalibrated
  difficulty tier; it would have shipped dead, then shipped wrong.
- **Running out of hearts silently destroyed the puzzle**, while the code comment
  beside it claimed the opposite.
- **Every solve burned two results reactions instead of one**, recycling a pool
  of seven after three solves — the "becomes wallpaper within one session"
  failure the copy pools exist to prevent.

All four passed the full suite. Three were caught by a human-style review of the
diff and one by a merge conflict; none by a test, and none could have been. They
share a shape: a seam crossed by two tickets that never saw each other.

So there are two problems, and the second is the durable one. The game has never
run on hardware — and the layer where its remaining bugs live has no way to fail
loudly.

## Solution

Close both, in that order, because the first is what tells you how big the
second needs to be.

**Get it running on a phone.** Install the iOS and Android build modules, take
the first Play-mode session, and fix what the eye finds. The layout arithmetic
in the skin and motion passes is unverified by construction: HUD strip bounds,
numpad rows, card geometry, board separators and the safe-area inset were all
reasoned about rather than looked at. Budget for that pass to find real
problems, and treat what it finds as data about which surfaces were hardest to
get right blind.

**Then give the Core↔presentation seam a way to fail.** A `Sudoku.Game.Tests`
PlayMode assembly, scoped narrowly and deliberately: not board rendering, not
pixel positions, not the input state machine — the *wiring*. Does a preference
reach every channel that should honour it. Does every path that creates a
session announce it. Does completing a puzzle clear its slot. Does the flow
reach the results card exactly once. Each of the four bugs above is a
two-line test at that seam, and each was invisible without one.

Spec 001's reasoning against a Unity harness was sound for what it evaluated —
rendering and input, where the cost is high and the protection is thin. It did
not evaluate wiring, which is cheap to test and, on the evidence, is where the
defects are. This is a correction to a scope decision, not a reversal of it.

**Finish the three deferred gaps** while the editor is open, because all three
are blocked on exactly that: TextMeshPro and the font atlases, CC0 audio to
replace the placeholder tones, and Reduce Motion on iOS.

---

## User Stories

### Playing it for real

1. As a player, I want to install the game on my phone and solve a puzzle end to end, so that the game exists.
2. As a player, I want the board and controls to fit my screen without clipping, overlapping, or stranding a control under the notch, so that I can actually play.
3. As a player, I want the interface to hold its frame rate while I tap and while the completion cascade runs, so that the game feels finished rather than cheap.
4. As a player, I want to hit the cell and the digit I aimed at with my thumb, so that input is not a source of mistakes.
5. As a player, I want text that looks like the game's own voice rather than a system default, so that the product feels designed.
6. As a player, I want sound effects that sound deliberate, so that the audio adds to the game rather than apologising for itself.
7. As a player on iOS, I want Reduce Motion honoured, so that playing does not make me nauseous.

### Developer-facing

8. As a developer, I want a test that fails when a setting stops reaching one of the channels that should honour it, so that a preference cannot half-work.
9. As a developer, I want a test that fails when a session is created without being announced, so that an analytics event cannot silently go nowhere.
10. As a developer, I want a test that fails when a completed or failed puzzle leaves its slot in the wrong state, so that a player cannot lose a board to a flow bug.
11. As a developer, I want a test that fails when the completion flow renders the results card more than once, so that a pool of copy is not spent at twice the intended rate.
12. As a developer, I want the PlayMode suite to run headlessly alongside the EditMode one, so that the wiring is checked on every change rather than when someone remembers.
13. As a developer, I want a repeatable device-build command, so that getting a build onto a phone is not an act of recall.
14. As a developer, I want the font atlases generated by a checked-in command rather than by hand, so that regenerating them is not archaeology.

---

## Implementation Decisions

### The device build

- Install the **iOS** and **Android** build modules via Unity Hub for 6000.5.10f1. Neither is present today; only `MacStandaloneSupport` and `WebGLSupport` are.
- iOS additionally needs **Xcode** and a signing identity. This is the one step in this spec that cannot be automated on behalf of the developer.
- Android ships **ARM64 only** (`AndroidTargetArchitectures: 2`, already set), `minSdk 26`, target SDK auto.
- A checked-in build command — an editor menu item and a `tools/` script — so a build is reproducible rather than remembered.
- The first Play-mode session is a **scheduled activity, not a formality**. Everything visual on `main` was authored blind.

### The `Sudoku.Game.Tests` seam

- A **PlayMode** assembly. It needs a scene, a canvas and a real `GameBootstrap`, which EditMode cannot give it.
- **Scope is the point.** It tests *wiring*: preferences reaching their channels, sessions being announced, slots reaching the right state, the completion flow running its stages once. It does **not** test rendering, pixel geometry, layout arithmetic, or the input state machine — spec 001's judgement that those cost more than they protect stands unchallenged.
- The four bugs listed in the Problem Statement are the **initial test cases**. A regression suite whose first entries are real, shipped-quality escapes is worth more than one written from imagination.
- It must run **headlessly** and join `tools/check.sh` as a fourth stage. A suite that only runs in the editor will not run.
- Expect it to be **slow relative to the Core suite** — seconds, not the current ~7s for 277 tests. That is acceptable at this size and is a reason to keep the scope narrow.

### TextMeshPro and the fonts

- Import TMP essential resources, then run the already-written, compile-verified `Sudoku/Theme/Generate Font Assets` command.
- **Both shipped fonts are variable-axis**, and TMP's runtime API exposes no variable-axis pinning, so atlases bake at each font's default instance. The spec asks for Fredoka SemiBold/Bold and Nunito Bold/ExtraBold — getting those requires **static font instances** exported per weight, or a Font Asset Creator pass per weight. Decide which and record it.
- Swap `Ui.Label` from `UnityEngine.UI.Text` to `TextMeshProUGUI`, sourcing the face from `ThemeDefinition.DisplayFont` / `.NumeralFont`.
- Nunito was chosen for board digits specifically for unambiguous numerals at note size. **Verify that on a device**, at note size, before accepting the atlas.

### Audio

- Replace the eight self-generated placeholder tones with genuinely CC0-licensed effects: place, erase, error, hint, box complete, puzzle complete, button tap, heart lost.
- Record real provenance — source, licence, and a link — in `Assets/_Project/Audio/README.md`. The current placeholders are documented as unlicensed and self-generated; that honesty is the thing to preserve, not the files.
- Filenames and import settings are unchanged, so this is a drop-in.

### Reduce Motion on iOS

- Requires a small Objective-C plugin wrapping `UIAccessibilityIsReduceMotionEnabled`; Unity 6000.5.10f1 does not surface it. Android already reads the real setting.
- `Assets/_Project/Plugins/iOS/SudokuHaptics.m` is **already in the repo, already unbuilt, and already unverified** — written during spec 001 with no iOS module installed. Build and verify it in the same pass; do not add a second unbuilt native file beside it.
- The damping switch and the Settings row already exist and work. This only replaces the iOS default's source.

### Carried-over defects

- **Bank exhaustion repeats the sequence.** `PuzzleLibrary` resets `Played` while keeping `Offset`, so a player who finishes 2,000 puzzles in a tier sees them again (spec 001 addendum 37). Practically unreachable, currently undecided rather than designed. Decide: reshuffle, extend the bank, or accept and document.
- **Undo history is unbounded during play**, as spec 001 requires, and capped at 200 only when persisted. Confirm on-device memory behaviour in a long session; this has never been measured under a real allocator.

---

## Testing Decisions

### What makes a good test here

The same rule as spec 001: assert **external behaviour at the highest available
seam**. What changes is that a second seam now exists.

`Sudoku.Core` keeps the overwhelming majority of the value and all of the rules.
Nothing about the Core suite changes.

The new PlayMode suite is small, narrow, and defined by what it must *not* test.
It exists because four defects crossed the Core↔presentation boundary and no
test could see them. Its job is that boundary and nothing else. If it starts
accumulating assertions about layout or rendering, it has drifted into the cost
spec 001 correctly refused to pay.

### Seams

1. **`GameSession` (EditMode)** — unchanged, and still the primary seam. All rules.
2. **`GameBootstrap` composition (PlayMode)** — the new one. A real object graph in a real scene: settings propagate to every channel, sessions are announced on every creation path, slots reach the right state on completion and failure, the completion flow runs each stage once.
3. **`SaveSerializer` (EditMode)** — unchanged. Round-trip and versioned fixtures.
4. **The device itself** — not a test seam, a *review* seam. Frame rate, thumb targets, notch behaviour and glyph legibility are judged by a human with a phone. Nothing here pretends otherwise.

### Prior art

Spec 001's EditMode conventions stand — fixed seeds, fixture puzzles as string
constants, property tests over volume, one assertion subject per test, snake_case
sentence test names. The PlayMode suite follows them where they apply and
establishes the setup/teardown convention for scene-based tests, which becomes
the prior art for anything later.

---

## Out of Scope

**Everything spec 001 put out of scope stays out of scope** — meta systems,
monetization, variants, alternate input, auto-notes, redo, localization beyond
the table, onboarding, accounts, cloud save, music, scoring, server content.

**Rendering and layout tests.** Explicitly. The device pass replaces them, and
spec 001's reasoning against them is accepted.

**An input-state-machine harness.** Same reason.

**Tablet and landscape layouts.** Portrait phone only, still.

**Difficulty recalibration.** The thresholds are data and the bake is
re-runnable by design, but there is no telemetry yet to recalibrate against.
That needs players.

**CI.** Running the suites on a hosted runner is worth doing and is not this
spec. `tools/check.sh` is the current gate.

---

## Further Notes

### Build order

1. Install the build modules. Get a portrait build onto an Android device — no signing identity required, so it is the shorter path to a first look.
2. First Play-mode session and the layout fix pass. **Expect real problems.** Everything visual was authored blind.
3. TextMeshPro, font atlases, static weights. Verify numeral legibility at note size on the device.
4. `Sudoku.Game.Tests`, seeded with the four known escapes, wired into `tools/check.sh`.
5. CC0 audio with recorded provenance.
6. iOS: Xcode, signing, build, the haptics plugin and the Reduce Motion read, verified together.
7. On-device pass against spec 001's definition of done, end to end.

Steps 1-3 are where the surprises are. Step 4 is what stops the next set of
surprises being silent.

### Known risks

- **The layout pass may be larger than it looks.** Six screens, a board, a numpad and a HUD were all positioned by arithmetic. If the first session finds the geometry substantially wrong, the honest response is to reserve real time for it rather than patch coordinates until it stops looking broken.
- **PlayMode tests are the kind of suite that rots.** They are slower, they need a scene, and they fail for environmental reasons. Keeping the scope to wiring is what keeps them trustworthy; the moment they become flaky, they will be ignored, and an ignored suite is worse than none.
- **Variable-font weights may not survive the atlas bake cleanly.** If static instances turn out to be awkward, the cut is to ship one weight per family rather than to ship blurry text — the type choice was made for legibility at note size, and that is the property to protect.
- **Sourcing eight CC0 effects that sound like one set** is a taste problem, not a licensing one. Effects assembled from different sources rarely cohere. Budget for auditioning more than eight.
