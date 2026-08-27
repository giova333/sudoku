using Sudoku.Core.Solving;

namespace Sudoku.Core.Session
{
    /// <summary>
    /// A nudge the player can act on: which cell, which digit, and - the part
    /// that matters - which cells make it inevitable, so the UI can show the
    /// reasoning instead of just handing over an answer.
    /// </summary>
    public sealed class Hint
    {
        public Hint(int cellIndex, int digit, Technique technique, int[] reasonCells)
        {
            CellIndex = cellIndex;
            Digit = digit;
            Technique = technique;
            ReasonCells = reasonCells;
        }

        public int CellIndex { get; }
        public int Digit { get; }

        /// <summary>The deduction that justifies it, for teaching the player.</summary>
        public Technique Technique { get; }

        public int[] ReasonCells { get; }
    }
}
