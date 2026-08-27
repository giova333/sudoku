using System;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using Sudoku.Core.Persistence;
using Sudoku.Core.Session;
using Sudoku.Game.Board;
using Sudoku.Game.Content;
using Sudoku.Game.Save;
using UnityEngine;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// Turns taps into player intent on the session, and the session's state
    /// back into pixels. It holds no rules of its own - every decision about
    /// what a move means lives in Sudoku.Core.
    /// </summary>
    public sealed class GamePresenter : MonoBehaviour
    {
        PuzzleLibrary _library;
        SaveStore _saves;
        BoardView _board;
        NumpadView _numpad;
        HudView _hud;

        GameSession _session;
        SaveSlot _slot;
        Puzzle _puzzle;
        DifficultyTier _tier = DifficultyTier.Easy;

        int _selected = -1;
        bool _notesMode;

        // Settings live here until the settings screen exists.
        bool _showMistakes = true;
        bool _timerVisible = true;

        public void Initialise(PuzzleLibrary library, SaveStore saves, BoardView board, NumpadView numpad,
            HudView hud)
        {
            _library = library;
            _saves = saves;
            _board = board;
            _numpad = numpad;
            _hud = hud;

            _board.CellTapped += OnCellTapped;
            _numpad.DigitTapped += OnDigitTapped;
            _numpad.DigitHeld += OnDigitHeld;
            _numpad.ActionTapped += OnAction;
            _hud.TierChosen += StartPuzzle;

            StartPuzzle(_tier);
        }

        void StartPuzzle(DifficultyTier tier)
        {
            if (_session != null && _session.Status == SessionStatus.InProgress)
                _session.Abandon();

            _tier = tier;

            // A half-finished puzzle of this difficulty outranks a fresh one:
            // starting a quick Easy game must never eat a stalled Expert.
            var waiting = _saves.Slot(tier);
            if (waiting != null && waiting.CanResume)
            {
                _slot = waiting;
                _puzzle = waiting.ToPuzzle();
                _session = waiting.ToSession();
            }
            else
            {
                _puzzle = _library.Next(tier, out var bankIndex);
                _slot = SaveSlot.ForTier(tier, PuzzleLibrary.BankName(tier), bankIndex, _puzzle,
                    RulesConfig.Default);
                _session = _slot.ToSession();
            }

            _session.Emitted += OnGameEvent;
            _session.Start();

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
            if (_saves == null || _slot == null) return;

            _slot.Session = _session.Capture();
            _saves.Put(_slot);
        }

        void Update()
        {
            if (_session == null) return;

            _session.Tick(Time.unscaledDeltaTime);
            _hud.Render(_session, _tier, _timerVisible);
        }

        void OnApplicationPause(bool paused)
        {
            if (_session == null) return;
            if (!paused)
            {
                _session.Resume();
                return;
            }

            // The process may never be scheduled again, so this write cannot
            // wait for a background thread.
            _session.Pause();
            Save();
            _saves.Flush();
        }

        void OnApplicationFocus(bool focused)
        {
            if (_session == null) return;
            if (focused)
            {
                _session.Resume();
                return;
            }

            _session.Pause();
            Save();
            _saves.Flush();
        }

        void OnCellTapped(int index)
        {
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
                    // Peek first so the cell we select is the cell that gets
                    // filled; both calls take the same preference.
                    var hint = _session.PeekHint(_selected);
                    if (hint != null && _session.UseHint(_selected))
                        _selected = hint.CellIndex;
                    break;
            }
            Save();
            Render();
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
