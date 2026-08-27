using System;
using Sudoku.Core.Session;
using Sudoku.Game.Board;

namespace Sudoku.Game.Audio
{
    /// <summary>
    /// Turns the session's event stream into sound and haptics.
    ///
    /// It listens rather than being called, so gameplay never learns that audio
    /// exists and a rule change that alters when a heart is lost changes what
    /// the player hears for free. The two things the stream does not announce -
    /// erasing, and a 3x3 box being finished - are handled either side of that:
    /// erase is played by the presenter that asked for it, and box completion
    /// is worked out here from the board the session already exposes.
    /// </summary>
    public sealed class GameAudio
    {
        readonly IAudioService _audio;

        /// <summary>Which boxes were already finished, so completion is heard once and not on every later move.</summary>
        readonly bool[] _boxComplete = new bool[BoardBoxes.Count];

        GameSession _session;
        int _hearts;

        public GameAudio(IAudioService audio)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        /// <summary>
        /// Listens to this session and stops listening to the last one. Dealing
        /// a new puzzle replaces the session object, and a listener left on the
        /// old one would go on playing a puzzle nobody can see.
        /// </summary>
        public void Follow(GameSession session)
        {
            if (_session != null) _session.Emitted -= OnGameEvent;

            _session = session;
            if (_session == null) return;

            _session.Emitted += OnGameEvent;
            _hearts = _session.HeartsRemaining;
            RefreshBoxes(false);
        }

        void OnGameEvent(GameEvent e)
        {
            switch (e.Kind)
            {
                case GameEventKind.PuzzleStarted:
                    // Restarting replays the same session object from its
                    // clues, so what "already finished" means has to be
                    // recounted rather than remembered.
                    _hearts = e.HeartsRemaining;
                    RefreshBoxes(false);
                    break;

                case GameEventKind.CellPlaced:
                    if (!e.WasCorrect) break;
                    _audio.Play(Sfx.Place);
                    _audio.Impact(Haptic.Light);
                    // A finished board is about to announce itself; one fanfare
                    // is enough, and the box under the last digit is implied.
                    RefreshBoxes(e.EmptyCellCount > 0);
                    break;

                case GameEventKind.MistakeMade:
                    // A mistake that cost a heart is a different event to the
                    // player than one that did not, so it gets its own sound
                    // rather than the same buzz twice.
                    _audio.Play(e.HeartsRemaining < _hearts ? Sfx.HeartLost : Sfx.Error);
                    _audio.Impact(Haptic.Firm);
                    _hearts = e.HeartsRemaining;
                    break;

                case GameEventKind.NoteToggled:
                    // Notes are speculation, so they get the quietest sound the
                    // game owns and no haptic at all - a player pencilling in
                    // half a grid should not feel it buzzing.
                    _audio.Play(Sfx.ButtonTap);
                    break;

                case GameEventKind.UndoUsed:
                    _audio.Play(Sfx.Erase);
                    RefreshBoxes(false);
                    break;

                case GameEventKind.HintUsed:
                    _audio.Play(Sfx.Hint);
                    _audio.Impact(Haptic.Light);
                    RefreshBoxes(e.EmptyCellCount > 0);
                    break;

                case GameEventKind.PuzzleCompleted:
                    _audio.Play(Sfx.PuzzleComplete);
                    _audio.Impact(Haptic.Light);
                    break;
            }
        }

        /// <summary>
        /// Recounts which boxes stand finished, and plays the chime for any
        /// that just became so.
        ///
        /// Every board change recounts all nine rather than only the box that
        /// was touched, because undo can un-finish one - and nine passes over
        /// nine cells, a few times a second at most, is not worth the
        /// bookkeeping needed to avoid it.
        /// </summary>
        void RefreshBoxes(bool announce)
        {
            for (var box = 0; box < BoardBoxes.Count; box++)
            {
                var complete = BoardBoxes.IsComplete(_session, box);
                if (complete && !_boxComplete[box] && announce)
                    _audio.Play(Sfx.BoxComplete);

                _boxComplete[box] = complete;
            }
        }
    }
}
