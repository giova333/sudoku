# Sudoku

A casual mobile Sudoku game. Portrait-only, iOS/Android. Sudoku is the core
mechanic; meta systems (daily challenge, currency, cosmetics, achievements),
IAP, and ads are planned but out of scope for the current milestone.

**Unity 6000.5.10f1 · URP · uGUI + TextMeshPro**

## Where to start

- **[docs/specs/001-core-sudoku-gameplay.md](docs/specs/001-core-sudoku-gameplay.md)** —
  the authoritative spec for the core milestone: problem, solution, 89 user
  stories, implementation decisions, test seams, out-of-scope, build order,
  glossary, and known risks. Read this before writing any code. Its addendum
  (items 1-38) records every deviation made while building it, so the spec
  stays an accurate description of what exists.
- **[docs/specs/001-implementation-log.md](docs/specs/001-implementation-log.md)** —
  which tickets landed, and what ticket #14 still owes.
- **[docs/specs/002-proving-it-on-a-device.md](docs/specs/002-proving-it-on-a-device.md)** —
  draft follow-up: get it running on a phone, then give the Core-to-presentation
  seam a way to fail.

## Layout

```
Assets/_Project/
  Core/         pure C# game rules — NO UnityEngine references, ever
  Core.Tests/   EditMode tests for Core
  Game/         Unity presentation layer (Board, Input, Session, Save,
                Theme, Screens, Audio, Analytics, Copy, Bootstrap)
  Editor/       editor tooling, incl. the puzzle bake pipeline
  Data/         baked puzzle banks, theme assets
  Fonts/        Fredoka (UI) + Nunito (board digits), both OFL
```

The `Core` → `Game` dependency direction is forbidden and enforced by the
assembly definition graph. All game rules live in `Core`; the Unity layer is
presentation and platform services only.

## Running the tests

`Sudoku.Core` is engine-free, so the suite runs headlessly:

```sh
/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$(pwd)" \
  -runTests -testPlatform EditMode -testResults ./TestResults.xml \
  -logFile - -quit
```

Close the Unity Editor first — batchmode cannot attach to an open project.
