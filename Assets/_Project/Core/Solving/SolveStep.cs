namespace Sudoku.Core.Solving
{
    /// <summary>
    /// One deduction a human could make from the current board: either a digit
    /// that can be placed, or candidates that can be eliminated.
    ///
    /// <see cref="ReasonCells"/> is what makes this double as a hint - the UI
    /// highlights those cells to show *why* the step follows, rather than just
    /// handing over an answer.
    /// </summary>
    public sealed class SolveStep
    {
        public SolveStep(Technique technique, int cellIndex, int digit, int[] reasonCells)
        {
            Technique = technique;
            CellIndex = cellIndex;
            Digit = digit;
            ReasonCells = reasonCells;
        }

        public Technique Technique { get; }

        /// <summary>The cell a digit can be placed in, or -1 for an elimination step.</summary>
        public int CellIndex { get; }

        /// <summary>The digit to place, or the digit being eliminated.</summary>
        public int Digit { get; }

        /// <summary>Cells the player should look at to see the deduction.</summary>
        public int[] ReasonCells { get; }

        /// <summary>Candidates this step removes, as (cellIndex, digit) pairs.</summary>
        public (int Cell, int Digit)[] Eliminations { get; set; }

        public bool IsPlacement => CellIndex >= 0 && Eliminations == null;
    }
}
