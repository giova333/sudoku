using System;
using System.Collections.Generic;
using Sudoku.Core.Commands;
using Sudoku.Core.Model;
using Sudoku.Core.Solving;

namespace Sudoku.Core.Session
{
    /// <summary>
    /// One play-through of one puzzle. This is the primary seam the game is
    /// tested through: it is driven by player-intent calls and observed through
    /// its exposed board state.
    ///
    /// Every board mutation is routed through a <see cref="BoardCommand"/> so
    /// that one player action always undoes as one step.
    /// </summary>
    public sealed class GameSession
    {
        /// <summary>
        /// How far back undo reaches. Deep enough that no player meets it in
        /// practice, shallow enough to keep a save payload small.
        /// </summary>
        public const int UndoHistoryLimit = 200;

        readonly Puzzle _puzzle;
        readonly RulesConfig _rules;
        readonly ConstraintSet _constraints;

        readonly int[] _values = new int[Board.CellCount];

        // One 9-bit mask per cell: bit (digit - 1) is set when that digit is
        // pencilled in. A mask is cheaper than a set and trivially serializable.
        readonly int[] _notes = new int[Board.CellCount];

        readonly List<BoardCommand> _history = new List<BoardCommand>();

        int _emptyCells;
        bool _started;

        public GameSession(Puzzle puzzle) : this(puzzle, RulesConfig.Default) { }

        public GameSession(Puzzle puzzle, RulesConfig rules)
            : this(puzzle, rules, ConstraintSet.Classic) { }

        public GameSession(Puzzle puzzle, RulesConfig rules, ConstraintSet constraints)
        {
            _puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));

            for (var i = 0; i < Board.CellCount; i++)
                _values[i] = puzzle.ClueAt(i);

            HeartsRemaining = rules.Hearts;
            HintsRemaining = rules.Hints;
            _emptyCells = CountEmpty();
        }

        /// <summary>
        /// Everything that happens during play. Analytics and, later, meta
        /// systems subscribe here; gameplay never knows who is listening.
        /// </summary>
        public event Action<GameEvent> Emitted;

        /// <summary>
        /// Announces that play has begun. Called once the listeners are
        /// attached, since a constructor has no audience yet.
        /// </summary>
        public void Start()
        {
            if (_started) return;
            _started = true;
            Emit(GameEventKind.PuzzleStarted);
        }

        /// <summary>The player left without finishing. Emits the drop-off event.</summary>
        public void Abandon()
        {
            if (Status != SessionStatus.InProgress) return;
            Emit(GameEventKind.PuzzleAbandoned);
        }

        void Emit(GameEventKind kind, int cellIndex = -1, int digit = Board.Empty, bool wasCorrect = false)
        {
            var handler = Emitted;
            if (handler == null) return;

            handler(new GameEvent(kind, cellIndex, digit, wasCorrect,
                HeartsRemaining, HintsRemaining, MistakeCount, HintsUsed,
                ElapsedSeconds, _emptyCells));
        }

        /// <summary>Where the play-through stands. Only InProgress accepts moves.</summary>
        public SessionStatus Status { get; private set; } = SessionStatus.InProgress;

        /// <summary>
        /// Seconds the player has actually spent solving. Accumulated from
        /// caller-supplied deltas rather than read from a clock, so changing the
        /// device time cannot alter a recorded time.
        /// </summary>
        public float ElapsedSeconds { get; private set; }

        public bool IsPaused { get; private set; }

        /// <summary>True while the timer should run and moves should be accepted.</summary>
        public bool IsActive => Status == SessionStatus.InProgress && !IsPaused;

        /// <summary>
        /// Advance the play clock. The caller passes an unscaled frame delta;
        /// time only accrues while the session is active.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (!IsActive || deltaSeconds <= 0f)
                return;

            ElapsedSeconds += deltaSeconds;
        }

        public void Pause() => IsPaused = true;

        public void Resume() => IsPaused = false;

        /// <summary>Wrong placements the player may still make this puzzle.</summary>
        public int HeartsRemaining { get; private set; }

        /// <summary>
        /// Every wrong placement the player has made, including repeats that
        /// cost no heart. Tracked separately from <see cref="HeartsRemaining"/>
        /// so perfect-solve statistics survive changes to the heart system.
        /// </summary>
        public int MistakeCount { get; private set; }

        /// <summary>The digit currently shown in a cell, or <see cref="Board.Empty"/>.</summary>
        public int ValueAt(int index) => _values[index];

        /// <summary>
        /// True when the cell holds a player digit that differs from the
        /// puzzle's unique solution. Mistakes are solution-based, not
        /// conflict-based: a digit that is merely legal but wrong is still a
        /// mistake, and is flagged the moment it is placed.
        /// </summary>
        public bool IsMistakeAt(int index)
        {
            var value = _values[index];
            return value != Board.Empty && value != _puzzle.SolutionAt(index);
        }

        /// <summary>True when <paramref name="digit"/> is pencilled into the cell.</summary>
        public bool HasNote(int index, int digit) => (_notes[index] & MaskOf(digit)) != 0;

        /// <summary>
        /// Player intent: put <paramref name="digit"/> into a cell.
        /// Returns false when the move was rejected and the board is unchanged.
        /// </summary>
        public bool Place(int index, int digit)
        {
            if (!IsActive)
                return false;
            if (_puzzle.IsGiven(index))
                return false;
            if (digit < 1 || digit > Board.Size)
                return false;

            // Re-entering the digit already showing is a no-op, so a
            // double-tap on a wrong digit never costs a second heart.
            if (_values[index] == digit)
                return false;

            var edits = new List<BoardEdit>
            {
                new BoardEdit(index, _values[index], digit, _notes[index], 0)
            };

            if (_rules.AutoRemoveNotes)
            {
                var keep = ~MaskOf(digit);
                foreach (var peer in _constraints.PeersOf(index))
                {
                    var before = _notes[peer];
                    var after = before & keep;
                    if (before != after)
                        edits.Add(new BoardEdit(peer, _values[peer], _values[peer], before, after));
                }
            }

            Commit(new BoardCommand(BoardCommandKind.Place, index, edits));

            var correct = digit == _puzzle.SolutionAt(index);
            _emptyCells = CountEmpty();
            Emit(GameEventKind.CellPlaced, index, digit, correct);

            if (!correct)
            {
                MistakeCount++;
                if (_rules.MistakeLimitEnabled && HeartsRemaining > 0)
                    HeartsRemaining--;

                Emit(GameEventKind.MistakeMade, index, digit, false);

                if (_rules.MistakeLimitEnabled && HeartsRemaining == 0)
                {
                    Status = SessionStatus.Failed;
                    Emit(GameEventKind.HeartsDepleted, index, digit, false);
                    return true;
                }
            }

            RefreshCompletion();
            return true;
        }

        /// <summary>
        /// Player intent: clear a cell. Returns false when there was nothing to
        /// clear, or the target is a clue. Erasing never costs a heart - the
        /// mistake was already paid for when it was placed.
        /// </summary>
        public bool Erase(int index)
        {
            if (!IsActive)
                return false;
            if (_puzzle.IsGiven(index))
                return false;
            if (_values[index] == Board.Empty && _notes[index] == 0)
                return false;

            Commit(new BoardCommand(BoardCommandKind.Erase, index, new List<BoardEdit>
            {
                new BoardEdit(index, _values[index], Board.Empty, _notes[index], 0)
            }));
            return true;
        }

        /// <summary>
        /// Player intent: add or remove a pencil mark. Notes are speculation,
        /// so they never count as a mistake and never cost a heart.
        /// </summary>
        public bool ToggleNote(int index, int digit)
        {
            if (!IsActive)
                return false;
            if (_puzzle.IsGiven(index))
                return false;
            if (digit < 1 || digit > Board.Size)
                return false;

            Commit(new BoardCommand(BoardCommandKind.ToggleNote, index, new List<BoardEdit>
            {
                new BoardEdit(index, _values[index], _values[index], _notes[index], _notes[index] ^ MaskOf(digit))
            }));
            Emit(GameEventKind.NoteToggled, index, digit);
            return true;
        }

        /// <summary>
        /// Player intent: reverse the last action. Returns false when there is
        /// nothing to undo. Undo never refunds a heart or a hint - if it did,
        /// the mistake system would be decorative.
        /// </summary>
        public bool Undo()
        {
            if (!IsActive)
                return false;
            if (_history.Count == 0)
                return false;

            var last = _history[_history.Count - 1];
            _history.RemoveAt(_history.Count - 1);
            last.Revert(_values, _notes);
            _emptyCells = CountEmpty();
            Emit(GameEventKind.UndoUsed, last.PrimaryIndex);
            return true;
        }

        /// <summary>Actions currently available to undo.</summary>
        public int UndoDepth => _history.Count;

        /// <summary>Hints the player may still take this puzzle.</summary>
        public int HintsRemaining { get; private set; }

        /// <summary>
        /// Hints taken. A solve with any hints is not a perfect solve, so this
        /// is recorded separately from the remaining count and survives undo.
        /// </summary>
        public int HintsUsed { get; private set; }

        /// <summary>
        /// The next deduction the player could be shown, without spending
        /// anything. Returns null when the session is over, hints are gone, or
        /// nothing can currently be deduced.
        ///
        /// When <paramref name="preferredCell"/> names an empty cell that is
        /// solvable right now, the hint answers the question the player is
        /// actually asking rather than picking for them.
        /// </summary>
        public Hint PeekHint(int preferredCell = -1)
        {
            if (!IsActive || HintsRemaining <= 0)
                return null;

            // Wrong digits already on the board would poison the deduction, so
            // hints reason about the player's correct progress only.
            var grid = new int[Board.CellCount];
            for (var i = 0; i < Board.CellCount; i++)
                grid[i] = _values[i] == _puzzle.SolutionAt(i) ? _values[i] : Board.Empty;

            var candidates = TechniqueSolver.BuildCandidates(grid, _constraints);

            if (preferredCell >= 0 && grid[preferredCell] == Board.Empty)
            {
                var preferred = HintForCell(grid, candidates, preferredCell);
                if (preferred != null)
                    return preferred;
            }

            for (var guard = 0; guard < 200; guard++)
            {
                var step = TechniqueSolver.NextStepForTesting(grid, candidates, _constraints);
                if (step == null)
                    break;

                if (step.IsPlacement)
                    return new Hint(step.CellIndex, step.Digit, step.Technique, step.ReasonCells);

                var progressed = false;
                foreach (var e in step.Eliminations)
                {
                    var before = candidates[e.Cell];
                    candidates[e.Cell] &= ~(1 << (e.Digit - 1));
                    if (before != candidates[e.Cell]) progressed = true;
                }
                if (!progressed)
                    break;
            }

            // Nothing deducible by known technique: fall back to the easiest
            // unsolved cell so a paying player is never left without help.
            for (var i = 0; i < Board.CellCount; i++)
                if (grid[i] == Board.Empty)
                    return new Hint(i, _puzzle.SolutionAt(i), Technique.NakedSingle,
                        FilledPeersOf(grid, i));

            return null;
        }

        Hint HintForCell(int[] grid, int[] candidates, int cell)
        {
            var step = TechniqueSolver.NextStepForTesting(grid, candidates, _constraints);
            var guard = 0;

            while (step != null && guard++ < 200)
            {
                if (step.IsPlacement)
                {
                    if (step.CellIndex == cell)
                        return new Hint(step.CellIndex, step.Digit, step.Technique, step.ReasonCells);
                    // Applying it may expose the cell the player asked about.
                    grid[step.CellIndex] = step.Digit;
                    candidates = TechniqueSolver.BuildCandidates(grid, _constraints);
                }
                else
                {
                    var progressed = false;
                    foreach (var e in step.Eliminations)
                    {
                        var before = candidates[e.Cell];
                        candidates[e.Cell] &= ~(1 << (e.Digit - 1));
                        if (before != candidates[e.Cell]) progressed = true;
                    }
                    if (!progressed) break;
                }

                step = TechniqueSolver.NextStepForTesting(grid, candidates, _constraints);
            }

            return null;
        }

        int[] FilledPeersOf(int[] grid, int index)
        {
            var reason = new List<int>();
            foreach (var peer in _constraints.PeersOf(index))
                if (grid[peer] != Board.Empty)
                    reason.Add(peer);
            return reason.ToArray();
        }

        /// <summary>
        /// Takes the next hint: fills the cell and spends one. A hint is never
        /// spent when there is nothing useful to reveal.
        ///
        /// Takes the same <paramref name="preferredCell"/> as
        /// <see cref="PeekHint"/> so the cell the player was shown is the cell
        /// that gets filled.
        /// </summary>
        public bool UseHint(int preferredCell = -1)
        {
            var hint = PeekHint(preferredCell);
            if (hint == null)
                return false;

            var edits = new List<BoardEdit>
            {
                new BoardEdit(hint.CellIndex, _values[hint.CellIndex], hint.Digit, _notes[hint.CellIndex], 0)
            };

            if (_rules.AutoRemoveNotes)
            {
                var keep = ~MaskOf(hint.Digit);
                foreach (var peer in _constraints.PeersOf(hint.CellIndex))
                {
                    var before = _notes[peer];
                    var after = before & keep;
                    if (before != after)
                        edits.Add(new BoardEdit(peer, _values[peer], _values[peer], before, after));
                }
            }

            Commit(new BoardCommand(BoardCommandKind.Hint, hint.CellIndex, edits));

            HintsRemaining--;
            HintsUsed++;
            _emptyCells = CountEmpty();
            Emit(GameEventKind.HintUsed, hint.CellIndex, hint.Digit, true);

            RefreshCompletion();
            return true;
        }

        /// <summary>
        /// The board is finished only when every cell holds its solution digit.
        /// A full board containing a wrong digit is not a win.
        /// </summary>
        void RefreshCompletion()
        {
            _emptyCells = CountEmpty();
            if (_emptyCells > 0)
                return;

            for (var i = 0; i < Board.CellCount; i++)
                if (_values[i] != _puzzle.SolutionAt(i))
                    return;

            Status = SessionStatus.Completed;
            Emit(GameEventKind.PuzzleCompleted);
        }

        int CountEmpty()
        {
            var n = 0;
            for (var i = 0; i < Board.CellCount; i++)
                if (_values[i] == Board.Empty)
                    n++;
            return n;
        }

        /// <summary>Cells still waiting for a digit.</summary>
        public int EmptyCellCount => _emptyCells;

        void Commit(BoardCommand command)
        {
            command.Apply(_values, _notes);
            _history.Add(command);
            if (_history.Count > UndoHistoryLimit)
                _history.RemoveAt(0);
        }

        static int MaskOf(int digit) => 1 << (digit - 1);
    }
}
