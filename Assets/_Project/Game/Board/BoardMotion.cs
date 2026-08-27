using System;
using Sudoku.Core.Session;
using Sudoku.Game.Motion;

namespace Sudoku.Game.Board
{
    /// <summary>
    /// Turns the session's event stream into movement on the board.
    ///
    /// It listens rather than being called, exactly as <see cref="Sudoku.Game.Audio.GameAudio"/>
    /// does and for the same reason: gameplay never learns that animation
    /// exists, and a rule change that alters when a mistake is a mistake changes
    /// what the player sees for free.
    ///
    /// Two things the stream cannot announce are handled either side of it. A
    /// finished 3x3 box is worked out here from the board the session already
    /// exposes; and an edit to a clue is a move the session *rejects*, so it
    /// never reaches the stream at all - the presenter that was told "no" calls
    /// <see cref="Refused"/>.
    /// </summary>
    public sealed class BoardMotion
    {
        readonly BoardView _board;

        /// <summary>Which boxes were already finished, so a box pops once rather
        /// than on every later move inside it.</summary>
        readonly bool[] _boxComplete = new bool[BoardBoxes.Count];

        GameSession _session;

        /// <summary>
        /// Whether a wrong digit is answered at all. The shake is the spec's
        /// redundant non-colour error signal, so it belongs behind the same
        /// switch as the underline: a player who has turned immediate mistake
        /// highlighting off has asked for a self-checked game, and a cell that
        /// jumps tells them exactly what the colour would have.
        ///
        /// It does not touch the mistake system - the heart is still spent and
        /// the run still ends at zero. Only the telling-off goes quiet.
        /// </summary>
        public bool AnnounceMistakes { get; set; } = true;

        public BoardMotion(BoardView board)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
        }

        /// <summary>
        /// Watches this session and stops watching the last one. Dealing a new
        /// puzzle replaces the session object, and a listener left on the old one
        /// would go on animating a board nobody can see.
        /// </summary>
        public void Follow(GameSession session)
        {
            if (_session != null) _session.Emitted -= OnGameEvent;

            _session = session;
            if (_session == null) return;

            _session.Emitted += OnGameEvent;
            RefreshBoxes(false);
        }

        /// <summary>
        /// The board turning a move down: a digit aimed at one of the puzzle's
        /// own clues. Nothing happened, so nothing is announced, and a rejection
        /// that looked identical to a tap on empty space would leave the player
        /// wondering whether the button worked.
        /// </summary>
        public void Refused(int index) => Motions.Shake(_board.CellAt(index).transform);

        /// <summary>
        /// Stage one of the completion flow: a diagonal sweep from the top-left
        /// corner to the bottom-right, one band of cells at a time.
        ///
        /// Diagonal rather than row by row because it reads as a wave crossing
        /// the board rather than as a list being processed, and this is the one
        /// moment in the game that is pure payoff.
        ///
        /// The continuation is called when the last band has finished rather than
        /// when the last tween is created, because the results card arriving on
        /// top of a sweep still in progress is the same as having no sweep.
        /// </summary>
        public void Cascade(Action done)
        {
            var band = Motions.Seconds(Motions.CascadeBand);
            var last = 0f;

            for (var row = 0; row < Core.Model.Board.Size; row++)
            for (var column = 0; column < Core.Model.Board.Size; column++)
            {
                var delay = (row + column) * band;
                if (delay > last) last = delay;

                Motions.Pop(_board.CellAt(row * Core.Model.Board.Size + column).transform,
                    Motions.CascadeStrength, delay);
            }

            Motions.After(last + Motions.Seconds(Motions.CascadePop), done);
        }

        void OnGameEvent(GameEvent e)
        {
            switch (e.Kind)
            {
                case GameEventKind.PuzzleStarted:
                    // Restarting replays the same session object from its clues,
                    // so what "already finished" means has to be recounted
                    // rather than remembered.
                    RefreshBoxes(false);
                    break;

                case GameEventKind.CellPlaced:
                    // A wrong digit is announced twice - once as a placement and
                    // once as a mistake - and the mistake is the one worth
                    // moving for.
                    if (!e.WasCorrect) break;
                    Motions.Pop(_board.CellAt(e.CellIndex).transform, Motions.PopStrength);
                    // A finished board is about to sweep the whole grid; one
                    // celebration is enough, and the box under the last digit is
                    // part of it either way.
                    RefreshBoxes(e.EmptyCellCount > 0);
                    break;

                case GameEventKind.MistakeMade:
                    if (!AnnounceMistakes) break;
                    Motions.Shake(_board.CellAt(e.CellIndex).transform);
                    break;

                case GameEventKind.HintUsed:
                    Motions.Pop(_board.CellAt(e.CellIndex).transform, Motions.PopStrength);
                    RefreshBoxes(e.EmptyCellCount > 0);
                    break;

                case GameEventKind.UndoUsed:
                    RefreshBoxes(false);
                    break;
            }
        }

        /// <summary>
        /// Recounts which boxes stand finished, and pops any that just became
        /// so.
        ///
        /// All nine are recounted on every board change rather than only the box
        /// that was touched, because undo can un-finish one - and nine passes
        /// over nine cells, a few times a second at most, costs less than the
        /// bookkeeping needed to avoid it.
        /// </summary>
        void RefreshBoxes(bool announce)
        {
            for (var box = 0; box < BoardBoxes.Count; box++)
            {
                var complete = BoardBoxes.IsComplete(_session, box);
                if (complete && !_boxComplete[box] && announce) PopBox(box);

                _boxComplete[box] = complete;
            }
        }

        /// <summary>
        /// Nine cells popping together, held back a little from the middle
        /// outwards so the box swells rather than flashing.
        /// </summary>
        void PopBox(int box)
        {
            var step = Motions.Seconds(Motions.CascadeBand);

            for (var slot = 0; slot < BoardBoxes.Size; slot++)
            {
                var ring = Math.Abs(slot / 3 - 1) + Math.Abs(slot % 3 - 1);
                Motions.Pop(_board.CellAt(BoardBoxes.Cell(box, slot)).transform,
                    Motions.BoxPopStrength, ring * step);
            }
        }
    }
}
