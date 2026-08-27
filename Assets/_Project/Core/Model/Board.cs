namespace Sudoku.Core.Model
{
    /// <summary>
    /// Dimensions and shared constants for a classic 9x9 board.
    /// Cells are addressed by a flat index: <c>index = row * Size + column</c>.
    /// </summary>
    public static class Board
    {
        /// <summary>Value stored in a cell that holds no digit.</summary>
        public const int Empty = 0;

        public const int Size = 9;
        public const int CellCount = Size * Size;
    }
}
