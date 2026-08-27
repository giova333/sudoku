using System;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using Sudoku.Core.Persistence;
using Sudoku.Core.Session;
using Sudoku.Game.Audio;
using Sudoku.Game.Board;
using Sudoku.Game.Content;
using Sudoku.Game.Save;
using Sudoku.Game.Settings;
using UnityEngine;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// Turns taps into player intent on the session, and the session's state
    /// back into pixels. It holds no rules of its own - every decision about
    /// what a move means lives in Sudoku.Core.
    /// </summary>
    public sealed class GamePresenter : MonoBehaviour, IScreen
    {
        PuzzleLibrary _library;
        SaveStore _saves;
        GameSettings _settings;
        RectTransform _root;
        BoardView _board;
        NumpadView _numpad;
        HudView _hud;
        IAudioService _audio;
        GameAudio _sounds;

        GameSession _session;
        SaveSlot _slot;
        Puzzle _puzzle;
        RulesConfig _rules;
        DifficultyTier _tier = DifficultyTier.Easy;

        int _selected = -1;
        bool _notesMode;

        public RectTransform Root => _root;

        /// <summary>
        /// Whether there is a puzzle worth going back to. Leaving the game
        /// screen suspends the session rather than ending it, so Home can offer
        /// to continue it.
        /// </summary>
        public bool HasSession => _session != null && _session.Status == SessionStatus.InProgress;

        /// <summary>
        /// Whether the game screen is the one on show. The clock must not run -
        /// and returning to the app must not restart it - on a session the
        /// player has left behind on Home.
        /// </summary>
        bool IsVisible => _root != null && _root.gameObject.activeInHierarchy;

        public void Initialise(PuzzleLibrary library, SaveStore saves, GameSettings settings,
            RectTransform root, BoardView board, NumpadView numpad, HudView hud,
            IAudioService audio)
        {
            _library = library;
            _saves = saves;
            _settings = settings;
            _root = root;
            _board = board;
            _numpad = numpad;
            _hud = hud;
            _audio = audio;

            // Almost everything audible is decided from the session's own event
            // stream rather than from here, so the presenter only has to hand
            // over each session it deals.
            if (_audio != null) _sounds = new GameAudio(_audio);

            _board.CellTapped += OnCellTapped;
            _numpad.DigitTapped += OnDigitTapped;
            _numpad.DigitHeld += OnDigitHeld;
            _numpad.ActionTapped += OnAction;

            _settings.Changed += OnSettingChanged;
        }

        /// <summary>
        /// Deals a puzzle of the given tier - or hands back the half-finished
        /// one already saved under it. The difficulty-select screen calls this
        /// on the way in; nothing starts a puzzle on launch, because launching
        /// lands on Home.
        /// </summary>
        public void StartPuzzle(DifficultyTier tier)
        {
            if (_session != null && _session.Status == SessionStatus.InProgress)
                _session.Abandon();

            _tier = tier;

            // A half-finished puzzle of this difficulty outranks a fresh one:
            // starting a quick Easy game must never eat a stalled Expert.
            var waiting = _saves != null ? _saves.Slot(tier) : null;
            if (waiting != null && waiting.CanResume)
            {
                _slot = waiting;

                // A resumed puzzle keeps the rules it was dealt under: the
                // mistake limit is a snapshot taken at deal time, and a
                // settings change must not rewrite a game already being scored.
                if (_slot.Rules == null) _slot.Rules = _settings.BuildRules();
                _rules = _slot.Rules;

                // Auto-removal is the one rule that is not a snapshot, so a
                // resumed puzzle picks up whatever the toggle says now.
                _rules.AutoRemoveNotes = _settings.AutoRemoveNotes.Value;

                _puzzle = _slot.ToPuzzle();
                _session = _slot.ToSession();
            }
            else
            {
                _rules = _settings.BuildRules();
                _puzzle = _library.Next(tier, out var bankIndex);
                _slot = SaveSlot.ForTier(tier, PuzzleLibrary.BankName(tier), bankIndex, _puzzle, _rules);
                _session = _slot.ToSession();
            }

            _session.Emitted += OnGameEvent;
            if (_sounds != null) _sounds.Follow(_session);
            _session.Start();

            _selected = -1;
            _notesMode = false;
            Save();
            Render();
        }

        /// <summary>
        /// Plays the same puzzle again from its clues. What "from the
        /// beginning" means to the board, the clock and every counter is the
        /// session's business, so this only forgets what the player was
        /// pointing at, writes the reset state down and redraws.
        /// </summary>
        public void Restart()
        {
            if (_session == null) return;

            _session.Restart();
            _selected = -1;
            _notesMode = false;
            Save();
            Render();
        }

        /// <summary>
        /// Autosave. It fires after every committed move because a mobile
        /// process is killed without warning - there is no later to write in.
        /// </summary>
        void Save()
        {
            if (_saves == null || _slot == null || _session == null) return;

            _slot.Session = _session.Capture();
            _saves.Put(_slot);
        }

        /// <summary>Leaving for another screen suspends the clock; coming back
        /// restarts it on the same session.</summary>
        public void OnShow()
        {
            if (_session != null) _session.Resume();
        }

        public void OnHide()
        {
            if (_session == null) return;

            _session.Pause();
            Save();
        }

        void Update()
        {
            if (_session == null || !IsVisible) return;

            _session.Tick(Time.unscaledDeltaTime);
            _hud.Render(_session, _tier, _settings.TimerVisible.Value);
        }

        void OnApplicationPause(bool paused)
        {
            if (_session == null) return;

            if (!paused)
            {
                // Only the clock is gated on visibility: a session the player
                // left behind on Home must not start ticking again just
                // because the app came back.
                if (IsVisible) _session.Resume();
                return;
            }

            // The save is not gated on anything. The process may never be
            // scheduled again, so this write cannot wait for a background
            // thread - and a suspended session sitting behind Home is still
            // the player's puzzle, so a hidden screen is no reason to skip it.
            _session.Pause();
            Save();
            Flush();
        }

        void OnApplicationFocus(bool focused)
        {
            if (_session == null) return;

            if (focused)
            {
                if (IsVisible) _session.Resume();
                return;
            }

            // Unconditional, for the same reason as above.
            _session.Pause();
            Save();
            Flush();
        }

        /// <summary>
        /// Puts the autosave on disk before returning. Only for pause and focus
        /// loss, where there may be no later.
        /// </summary>
        void Flush()
        {
            if (_saves != null) _saves.Flush();
        }

        void OnCellTapped(int index)
        {
            // Looking somewhere else answers "no" to a revealed hint, and a
            // hint that is never taken is never spent.
            _session.CancelHint();

            _selected = index;
            Render();
        }

        void OnDigitTapped(int digit)
        {
            if (_selected < 0) return;

            if (_notesMode) _session.ToggleNote(_selected, digit);
            else _session.Place(_selected, digit);

            Save();
            Render();
        }

        void OnDigitHeld(int digit)
        {
            if (_selected < 0) return;
            _session.ToggleNote(_selected, digit);
            Save();
            Render();
        }

        void OnAction(PadAction action)
        {
            // Every other button is the player changing the subject, which
            // drops a revealed hint without charging for it.
            if (action != PadAction.Hint)
                _session.CancelHint();

            switch (action)
            {
                case PadAction.Undo:
                    _session.Undo();
                    break;
                case PadAction.Erase:
                    // Erasing is the one move the session does not announce, so
                    // it is the one move the presenter has to give a voice to.
                    if (_selected >= 0 && _session.Erase(_selected)) Play(Sfx.Erase);
                    break;
                case PadAction.Notes:
                    _notesMode = !_notesMode;
                    Play(Sfx.ButtonTap);
                    break;
                case PadAction.Hint:
                    TapHint();
                    break;
            }

            Save();
            Render();
        }

        /// <summary>
        /// The hint button is one button doing two jobs: the first tap shows
        /// the cell and the reasoning for free, the second takes it. The
        /// session owns which of the two the next tap is, so the two halves
        /// can never disagree about the cell.
        /// </summary>
        void TapHint()
        {
            if (_session.PendingHint != null)
            {
                _session.TakeHint();
                return;
            }

            var revealed = _session.RevealHint(_selected);
            if (revealed == null) return;

            _selected = revealed.CellIndex;

            // Revealing is free, so the session says nothing about it. The tap
            // still has to be answered, or half the gesture is silent.
            Play(Sfx.ButtonTap);
        }

        /// <summary>Plays an effect the event stream cannot announce for us.</summary>
        void Play(Sfx effect)
        {
            if (_audio != null) _audio.Play(effect);
        }

        void Render()
        {
            _board.Render(_session, _puzzle, _selected, _settings.HighlightMistakes.Value);
            _numpad.Render(_session, _notesMode);
            _hud.Render(_session, _tier, _settings.TimerVisible.Value);
        }

        /// <summary>
        /// A preference change reaches the puzzle already in play, because a
        /// toggle that appears to do nothing reads as a broken toggle.
        ///
        /// The one exception is the mistake limit, which <see cref="GameSettings.BuildRules"/>
        /// snapshots at deal time - the settings screen says so rather than
        /// leaving the player to wonder.
        /// </summary>
        void OnSettingChanged(IPreference preference)
        {
            if (_session == null) return;

            // The session reads auto-removal from the rules object it was dealt
            // on every placement, so writing to that same object is what puts
            // the change into the puzzle in hand. It is the slot's rules object
            // too, so the change is saved with the puzzle rather than lost.
            _rules.AutoRemoveNotes = _settings.AutoRemoveNotes.Value;
            Save();

            Render();
        }

        /// <summary>
        /// The console stands in for analytics until the real service lands, but
        /// the event stream it listens to is the shipping one.
        /// </summary>
        static void OnGameEvent(GameEvent e)
        {
            switch (e.Kind)
            {
                case GameEventKind.PuzzleCompleted:
                    Debug.Log($"[sudoku] completed in {e.ElapsedSeconds:F0}s, " +
                              $"{e.MistakeCount} mistakes, {e.HintsUsed} hints");
                    break;
                case GameEventKind.HeartsDepleted:
                    Debug.Log("[sudoku] out of hearts");
                    break;
            }
        }
    }
}
