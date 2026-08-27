using System;
using System.Collections.Generic;
using Sudoku.Core.Session;

namespace Sudoku.Core.Analytics
{
    /// <summary>
    /// Turns the gameplay event stream into analytics events.
    ///
    /// This is the only translator, and it sits outside gameplay on purpose:
    /// <see cref="GameSession"/> announces what happened and never learns that
    /// anyone is listening, so there is not one analytics call anywhere in the
    /// rules. Adding an event later is a case in one switch here, not an edit to
    /// the code that plays sudoku.
    ///
    /// It is engine-free so the schema, the batching and the common parameters
    /// are all testable at the same seam the rules are.
    /// </summary>
    public sealed class AnalyticsReporter
    {
        /// <summary>
        /// How many placements one cell_placed event stands for.
        ///
        /// Placements outnumber every other event by an order of magnitude -
        /// roughly fifty per puzzle against a handful of everything else - and
        /// an SDK billed or throttled per event should not spend that budget on
        /// the least interesting thing a player does. Ten keeps a solve to
        /// about five events while still being fine-grained enough to see where
        /// in a puzzle a player slowed down.
        /// </summary>
        public const int CellPlacementBatchSize = 10;

        readonly IAnalyticsService _service;
        readonly List<AnalyticsParameter> _scratch = new List<AnalyticsParameter>();

        GameSession _session;

        int _placed;
        int _placedCorrect;
        float _placedElapsed;
        int _placedFilled;

        public AnalyticsReporter(IAnalyticsService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>The parameters attached to every event this reporter sends.</summary>
        public AnalyticsContext Context { get; } = new AnalyticsContext();

        /// <summary>
        /// Listens to a session, and stops listening to the one before it. The
        /// previous session's part-full batch goes out first, so leaving a
        /// puzzle never silently drops the last few moves made in it.
        /// </summary>
        public void Observe(GameSession session)
        {
            if (_session == session) return;

            FlushPlacements();

            if (_session != null) _session.Emitted -= OnGameEvent;
            _session = session;
            if (_session != null) _session.Emitted += OnGameEvent;
        }

        /// <summary>
        /// The player is now looking at this screen. Navigation is not a
        /// gameplay event, so it arrives here directly rather than through the
        /// session's stream.
        /// </summary>
        public void ScreenViewed(string screen)
        {
            FlushPlacements();
            Send("screen_viewed", AnalyticsParameter.Of("screen", screen));
        }

        /// <summary>A preference was changed, whichever one it was.</summary>
        public void SettingChanged(string setting, string value)
        {
            FlushPlacements();
            Send("setting_changed",
                AnalyticsParameter.Of("setting", setting),
                AnalyticsParameter.Of("value", value));
        }

        /// <summary>
        /// Sends the batch in hand and asks the backend to do the same. For
        /// backgrounding, where the process may never be scheduled again.
        /// </summary>
        public void Flush()
        {
            FlushPlacements();
            _service.Flush();
        }

        void OnGameEvent(GameEvent e)
        {
            if (e.Kind == GameEventKind.CellPlaced)
            {
                Batch(e);
                return;
            }

            // Everything else empties the batch first, so the recorded order is
            // the order things happened in: the placement that finished the
            // puzzle is reported before the completion, and the wrong digit
            // before the mistake it caused.
            FlushPlacements();

            switch (e.Kind)
            {
                case GameEventKind.PuzzleStarted:
                    Send("puzzle_started",
                        AnalyticsParameter.Of("elapsed_seconds", e.ElapsedSeconds),
                        AnalyticsParameter.Of("filled_cells", e.FilledCellCount),
                        AnalyticsParameter.Of("empty_cells", e.EmptyCellCount));
                    break;

                case GameEventKind.MistakeMade:
                    Send("mistake_made",
                        AnalyticsParameter.Of("cell", e.CellIndex),
                        AnalyticsParameter.Of("digit", e.Digit),
                        AnalyticsParameter.Of("mistake_count", e.MistakeCount),
                        AnalyticsParameter.Of("hearts_remaining", e.HeartsRemaining),
                        AnalyticsParameter.Of("elapsed_seconds", e.ElapsedSeconds));
                    break;

                case GameEventKind.NoteToggled:
                    Send("note_toggled",
                        AnalyticsParameter.Of("cell", e.CellIndex),
                        AnalyticsParameter.Of("digit", e.Digit));
                    break;

                case GameEventKind.UndoUsed:
                    Send("undo_used",
                        AnalyticsParameter.Of("cell", e.CellIndex),
                        AnalyticsParameter.Of("elapsed_seconds", e.ElapsedSeconds));
                    break;

                case GameEventKind.HintUsed:
                    Send("hint_used",
                        AnalyticsParameter.Of("cell", e.CellIndex),
                        AnalyticsParameter.Of("hints_used", e.HintsUsed),
                        AnalyticsParameter.Of("hints_remaining", e.HintsRemaining),
                        AnalyticsParameter.Of("elapsed_seconds", e.ElapsedSeconds));
                    break;

                case GameEventKind.HeartsDepleted:
                    Send("hearts_depleted",
                        AnalyticsParameter.Of("mistake_count", e.MistakeCount),
                        AnalyticsParameter.Of("filled_cells", e.FilledCellCount),
                        AnalyticsParameter.Of("elapsed_seconds", e.ElapsedSeconds));
                    break;

                case GameEventKind.PuzzleCompleted:
                    Send("puzzle_completed",
                        AnalyticsParameter.Of("elapsed_seconds", e.ElapsedSeconds),
                        AnalyticsParameter.Of("mistake_count", e.MistakeCount),
                        AnalyticsParameter.Of("hints_used", e.HintsUsed),
                        AnalyticsParameter.Of("hearts_remaining", e.HeartsRemaining));
                    break;

                // The earliest signal that a tier is graded wrong, which is why
                // it carries how long the player lasted and how far they got:
                // a tier abandoned late and nearly finished is a different
                // problem from one abandoned on sight.
                case GameEventKind.PuzzleAbandoned:
                    Send("puzzle_abandoned",
                        AnalyticsParameter.Of("elapsed_seconds", e.ElapsedSeconds),
                        AnalyticsParameter.Of("filled_cells", e.FilledCellCount),
                        AnalyticsParameter.Of("empty_cells", e.EmptyCellCount),
                        AnalyticsParameter.Of("mistake_count", e.MistakeCount),
                        AnalyticsParameter.Of("hints_used", e.HintsUsed));
                    break;
            }
        }

        void Batch(GameEvent e)
        {
            _placed++;
            if (e.WasCorrect) _placedCorrect++;

            // The batch is described by where it ended, so a run of placements
            // is still pinned to a point in the solve.
            _placedElapsed = e.ElapsedSeconds;
            _placedFilled = e.FilledCellCount;

            if (_placed >= CellPlacementBatchSize)
                FlushPlacements();
        }

        void FlushPlacements()
        {
            if (_placed == 0) return;

            var count = _placed;
            var correct = _placedCorrect;
            _placed = 0;
            _placedCorrect = 0;

            Send("cell_placed",
                AnalyticsParameter.Of("count", count),
                AnalyticsParameter.Of("correct", correct),
                AnalyticsParameter.Of("wrong", count - correct),
                AnalyticsParameter.Of("filled_cells", _placedFilled),
                AnalyticsParameter.Of("elapsed_seconds", _placedElapsed));
        }

        /// <summary>
        /// Attaches the common parameters and hands the event over. Two small
        /// allocations per event, which is affordable precisely because
        /// placements are batched - what is left is a handful of events per
        /// puzzle, not one per tap.
        /// </summary>
        void Send(string name, params AnalyticsParameter[] own)
        {
            _scratch.Clear();
            Context.WriteTo(_scratch);
            if (own != null) _scratch.AddRange(own);

            _service.Track(new AnalyticsEvent(name, _scratch.ToArray()));
        }
    }
}
