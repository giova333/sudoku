using System.Collections.Generic;
using Sudoku.Core.Commands;
using Sudoku.Core.Model;

namespace Sudoku.Core.Session
{
    /// <summary>
    /// Everything a <see cref="GameSession"/> needs to be put back exactly as
    /// the player left it. It exists because a mobile process is killed without
    /// warning: a resume has to be reconstructible from data alone, never from
    /// replaying the moves that produced it.
    ///
    /// Deliberately a plain carrier of value types, so the save layer can write
    /// one without knowing a single rule of the game.
    /// </summary>
    public sealed class SessionSnapshot
    {
        public SessionSnapshot()
        {
            Values = new int[Board.CellCount];
            Notes = new int[Board.CellCount];
            History = new List<BoardCommand>();
        }

        /// <summary>The digit showing in each of the 81 cells.</summary>
        public int[] Values { get; set; }

        /// <summary>One 9-bit pencil-mark mask per cell, in the same order.</summary>
        public int[] Notes { get; set; }

        /// <summary>
        /// The undo stack, oldest first - the order
        /// <see cref="GameSession.Undo"/> unwinds it from the back.
        /// </summary>
        public List<BoardCommand> History { get; set; }

        public float ElapsedSeconds { get; set; }

        public int HeartsRemaining { get; set; }

        public int HintsRemaining { get; set; }

        public int HintsUsed { get; set; }

        public int MistakeCount { get; set; }

        public SessionStatus Status { get; set; }

        public bool IsPaused { get; set; }

        /// <summary>
        /// Whether <see cref="GameSession.Start"/> has already fired. Carried so
        /// that resuming a puzzle does not announce it as a second start and
        /// double-count it in analytics.
        /// </summary>
        public bool Started { get; set; }
    }
}
