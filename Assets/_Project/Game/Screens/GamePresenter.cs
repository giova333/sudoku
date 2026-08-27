using System;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using Sudoku.Core.Session;
using Sudoku.Game.Board;
using Sudoku.Game.Content;
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
        RectTransform _root;
        BoardView _board;
        NumpadView _numpad;
        HudView _hud;

        GameSession _session;
        Puzzle _puzzle;
        DifficultyTier _tier = DifficultyTier.Easy;

        int _selected = -1;
        bool _notesMode;

        // Settings live here until the settings screen exists.
        bool _showMistakes = true;
        bool _timerVisible = true;

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

        public void Initialise(PuzzleLibrary library, RectTransform root, BoardView board,
            NumpadView numpad, HudView hud)
        {
            _library = library;
            _root = root;
            _board = board;
            _numpad = numpad;
            _hud = hud;

            _board.CellTapped += OnCellTapped;
            _numpad.DigitTapped += OnDigitTapped;
            _numpad.DigitHeld += OnDigitHeld;
            _numpad.ActionTapped += OnAction;
        }

        /// <summary>
        /// Deals a puzzle of the given tier. The difficulty-select screen calls
        /// this on the way in; nothing starts a puzzle on launch, because
        /// launching lands on Home.
        /// </summary>
        public void StartPuzzle(DifficultyTier tier)
        {
            if (_session != null && _session.Status == SessionStatus.InProgress)
                _session.Abandon();

            _tier = tier;
            _puzzle = _library.Next(tier);

            _session = new GameSession(_puzzle, RulesConfig.Default);
            _session.Emitted += OnGameEvent;
            _session.Start();

            _selected = -1;
            _notesMode = false;
            Render();
        }

        /// <summary>
        /// Plays the same puzzle again from its clues. What "from the
        /// beginning" means to the board, the clock and every counter is the
        /// session's business, so this only forgets what the player was
        /// pointing at and redraws.
        /// </summary>
        public void Restart()
        {
            if (_session == null) return;

            _session.Restart();
            _selected = -1;
            _notesMode = false;
            Render();
        }

        /// <summary>Leaving for another screen suspends the clock; coming back
        /// restarts it on the same session.</summary>
        public void OnShow()
        {
            if (_session != null) _session.Resume();
        }

        public void OnHide()
        {
            if (_session != null) _session.Pause();
        }

        void Update()
        {
            if (_session == null || !IsVisible) return;

            _session.Tick(Time.unscaledDeltaTime);
            _hud.Render(_session, _tier, _timerVisible);
        }

        void OnApplicationPause(bool paused)
        {
            if (_session == null) return;
            if (paused) _session.Pause();
            else if (IsVisible) _session.Resume();
        }

        void OnApplicationFocus(bool focused)
        {
            if (_session == null) return;
            if (focused && IsVisible) _session.Resume();
            else _session.Pause();
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

            Render();
        }

        void OnDigitHeld(int digit)
        {
            if (_selected < 0) return;
            _session.ToggleNote(_selected, digit);
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
                    if (_selected >= 0) _session.Erase(_selected);
                    break;
                case PadAction.Notes:
                    _notesMode = !_notesMode;
                    break;
                case PadAction.Hint:
                    TapHint();
                    break;
            }
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
            if (revealed != null)
                _selected = revealed.CellIndex;
        }

        void Render()
        {
            _board.Render(_session, _puzzle, _selected, _showMistakes);
            _numpad.Render(_session, _notesMode);
            _hud.Render(_session, _tier, _timerVisible);
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
