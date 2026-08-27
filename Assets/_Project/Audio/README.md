# Audio assets

## Where the files live

The clips and the mixer are under `Assets/_Project/Resources/Audio/`, not here.
They have to be in a `Resources` folder because the whole interface is built in
code at runtime (`GameBootstrap`) with no scene or prefab to hold an asset
reference, so `AudioService` loads them by name. This folder holds the
documentation for them.

```
Assets/_Project/Resources/Audio/
  Sudoku.mixer          Master -> SFX, Music. Exposed: MasterVolume, SfxVolume, MusicVolume
  place.wav
  erase.wav
  error.wav
  hint.wav
  box-complete.wav
  puzzle-complete.wav
  button-tap.wav
  heart-lost.wav
```

File names are the `Sudoku.Game.Audio.Sfx` enum in lower-kebab-case. Adding a
member to that enum and dropping a matching `.wav` in that folder is the whole
of adding a sound; `AudioService.FileNameOf` carries the four names where the
casing does not fall out automatically.

## Provenance - READ THIS BEFORE SHIPPING

**The eight `.wav` files currently committed are not CC0 assets. They are
synthetic placeholders generated programmatically for this branch** - short
sine and filtered-noise envelopes written by a script, with no external source
and no licence attached to them. They exist so that the service, the mixer
routing, the mutes and every call site can be built, heard and reviewed without
inventing a provenance that does not exist.

Sourcing real CC0 effects is still outstanding. Nothing in the code changes when
they arrive: drop the replacements in `Resources/Audio/` under the same eight
names and record the source and licence in the table below.

| Slot | Fired by | What it wants |
|---|---|---|
| `place` | a correct digit landing | short, soft, mid-register; heard ~50x per puzzle, so it must not tire |
| `erase` | clearing a cell, and undo | short, downward; the opposite gesture to `place` |
| `error` | a wrong digit that cost no heart | low and dull; wrong, but not punishing |
| `hint` | a hint filling a cell | rising; a question being answered |
| `box-complete` | a 3x3 box finished correctly | a small contained chime, clearly smaller than the puzzle fanfare |
| `puzzle-complete` | the grid solved | the only real fanfare in the game; ~1s |
| `button-tap` | any chrome button, notes toggle, hint reveal | the shortest thing that still reads as a press |
| `heart-lost` | a wrong digit that did cost a heart | falling; a loss, audibly heavier than `error` |

Every slot is optional at runtime. A missing clip makes `AudioService.Play` a
no-op rather than an error, so replacing them one at a time is safe.

## Mixer

`Sudoku.mixer` was authored in the Unity editor and has three groups: `Master`,
and `SFX` and `Music` under it. Every effect plays through `SFX`.

`Music` is empty and stays empty - **no music ships**. Most Sudoku players mute
it before they finish their first puzzle. The group exists anyway so that if
that decision is ever revisited it is a routing change rather than a rewrite of
the service.

The exposed parameters are the mutes. `AudioService.SoundEnabled` writes
`SfxVolume` (0 dB audible, -80 dB silent) so that anything already sounding goes
quiet with the switch, and gates `Play` as well so the mute still holds if the
mixer asset is ever missing.

## Silent switch and haptics

The iOS ringer switch is respected by the audio session Unity installs: with the
iOS player setting **Mute Other Audio Sources** off (`muteOtherAudioSources: 0`
in `ProjectSettings/ProjectSettings.asset`), the build uses
`AVAudioSessionCategoryAmbient`, which mixes with whatever the player was
already listening to and goes quiet when the switch is flipped. Nothing in the
game overrides the category, and nothing should.

Haptics are not audio and do not go through the mixer. They are a separate mute
on the same service, because the reason to silence a puzzle game is usually a
reason to keep it buzzing quietly - or the exact opposite. See
`Assets/_Project/Game/Audio/Haptics.cs` and
`Assets/_Project/Plugins/iOS/SudokuHaptics.m`.
