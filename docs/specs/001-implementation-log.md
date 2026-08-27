# Spec 001 — Implementation log

Tracks the ticket graph for
[`001-core-sudoku-gameplay.md`](001-core-sudoku-gameplay.md) through steps 5-8
of that spec's build order. Steps 1-4 landed on `main` before this branch.

## Ticket graph

```
#1 app shell ────┬──> #4 settings ──┬──> #8 typography+theming ──> #9 skin ──┬──> #10 motion
                 │                  ├──> #11 audio+haptics ─────────────────┤
                 ├──> #5 pause      └──> #13 analytics (also needs #1)      └──> #14 device build
                 ├──> #6 continue (also needs #2)
                 └──> #7 results   (also needs #2) ──> #12 copy voice
#2 save/resume ──┘
#3 two-tap hint  (independent)
```

| # | Ticket | Blocked by | Status |
|---|---|---|---|
| 1 | App shell: screen navigation, Home and Difficulty Select | — | Done — `ticket/01-app-shell` |
| 2 | Save and resume an in-progress puzzle | — | Done — `ticket/02-save-resume` |
| 3 | Two-tap technique hint | — | Done |
| 4 | Settings screen with persisted preferences | #1 | Done — `ticket/04-settings` |
| 5 | Pause screen | #1 | Done — `ticket/05-pause` |
| 6 | Continue, and knowing where you left off | #1, #2 | Done — `ticket/06-continue` |
| 7 | Results card, game over, and the consumable seam | #1, #2 | Done — `ticket/07-results` |
| 8 | Typography and theming | #4 | Done — `ticket/08-theming` (theme system complete; TMP migration and font atlases outstanding, see addendum 27) |
| 9 | Chunky-playful skin pass | #8 | Done — `ticket/09-skin` |
| 10 | Motion and juice | #9 | Done — `ticket/10-motion` (Reduce Motion is read from the OS on Android only; see addendum 33) |
| 11 | Audio and haptics | #4 | Done — `ticket/11-audio` (clips are placeholders; CC0 sourcing outstanding) |
| 12 | Copy voice table | #7 | Done — `ticket/12-copy` |
| 13 | Analytics interface and event wiring | #1, #4 | Done — `ticket/13-analytics` |
| 14 | Device build on iOS and Android | #9, #11 | **Not done — blocked on hardware.** See below. |

## Ticket #14 — what is done and what is blocked

The identity and orientation half of #14 is done; the build half cannot be done
from this machine.

| Acceptance criterion | Status |
|---|---|
| Bundle identifier is `com.hladunoleksandr.sudoku`, no placeholder identity | Done — Android and iPhone were already correct; `Standalone` still held the URP template's identifier and now matches |
| Portrait builds | Config done — portrait on, upside-down and both landscape off |
| Safe area correct on a notched device | Implemented (addendum 31), **never rendered** |
| Builds run on physical iOS and Android hardware | **Blocked** |
| Frame rate holds during interaction and the completion cascade | **Blocked** |
| Touch targets comfortable at real thumb size | **Blocked** |

The Unity install carries only `MacStandaloneSupport` and `WebGLSupport` in its
`PlaybackEngines`, so there is no iOS or Android build target to build for -
this is a Unity Hub install step, and iOS additionally needs Xcode and a signing
identity. The three blocked criteria need a device in a hand.

## What has never been rendered

Worth stating plainly, because the green suite does not cover it. All 277 tests
are `Sudoku.Core`; `Sudoku.Game` is compile-checked but never executed, and no
part of this branch has been run in the editor. The skin, the motion, the safe
area, and every screen layout were verified by compile-check and arithmetic
only. The first Play-mode session should expect to find layout problems.
