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
    /// comes from a <see cref="BoxWatcher"/>, the same one the board's own
    /// celebration is built on.
    /// </summary>
    public sealed class GameAudio
    {
        readonly IAudioService _audio;

        /// <summary>The chime's half of a finished 3x3 box - the swell is the
        /// other half, and both hang off one <see cref="BoxWatcher"/> so they
        /// can never land on different moves.</summary>
        readonly BoxWatcher _boxes;

        GameSession _session;
        int _hearts;

        /// <summary>
        /// Whether a wrong digit is answered out loud. Off, the buzz and the
        /// firm impact go with the colour and the shake: immediate mistake
        /// feedback is one announcement in several channels, and silencing one
        /// of them while the rest still shout is no setting at all.
        ///
        /// The heart is still spent and the run still ends at zero - this hides
        /// the feedback, it does not disable the mistake system.
        /// </summary>
        public bool AnnounceMistakes { get; set; } = true;

        public GameAudio(IAudioService audio)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _boxes = new BoxWatcher(_ => _audio.Play(Sfx.BoxComplete));
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
            _boxes.Follow(_session);
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
                    _boxes.Refresh(false);
                    break;

                case GameEventKind.CellPlaced:
                    if (!e.WasCorrect) break;
                    _audio.Play(Sfx.Place);
                    _audio.Impact(Haptic.Light);
                    // A finished board is about to announce itself; one fanfare
                    // is enough, and the box under the last digit is implied.
                    _boxes.Refresh(e.EmptyCellCount > 0);
                    break;

                case GameEventKind.MistakeMade:
                    // The count is kept either way, so turning the sound back on
                    // mid-puzzle picks up from the hearts that are actually left
                    // rather than from the last one that was heard.
                    var lost = e.HeartsRemaining < _hearts;
                    _hearts = e.HeartsRemaining;
                    if (!AnnounceMistakes) break;

                    // A mistake that cost a heart is a different event to the
                    // player than one that did not, so it gets its own sound
                    // rather than the same buzz twice.
                    _audio.Play(lost ? Sfx.HeartLost : Sfx.Error);
                    _audio.Impact(Haptic.Firm);
                    break;

                case GameEventKind.NoteToggled:
                    // Notes are speculation, so they get the quietest sound the
                    // game owns and no haptic at all - a player pencilling in
                    // half a grid should not feel it buzzing.
                    _audio.Play(Sfx.ButtonTap);
                    break;

                case GameEventKind.UndoUsed:
                    _audio.Play(Sfx.Erase);
                    _boxes.Refresh(false);
                    break;

                case GameEventKind.HintUsed:
                    _audio.Play(Sfx.Hint);
                    _audio.Impact(Haptic.Light);
                    _boxes.Refresh(e.EmptyCellCount > 0);
                    break;

                case GameEventKind.PuzzleCompleted:
                    _audio.Play(Sfx.PuzzleComplete);
                    _audio.Impact(Haptic.Light);
                    break;
            }
        }
    }
}
