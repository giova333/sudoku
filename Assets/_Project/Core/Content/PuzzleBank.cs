using System;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;

namespace Sudoku.Core.Content
{
    /// <summary>
    /// A baked, graded collection of puzzles, read straight out of its bytes.
    /// Nothing is parsed up front: a puzzle is decoded only when it is asked
    /// for, so loading a bank costs nothing but the file read.
    /// </summary>
    public sealed class PuzzleBank
    {
        readonly byte[] _bytes;
        readonly int _offset;

        internal PuzzleBank(byte[] bytes, int offset, DifficultyTier tier, int count)
        {
            _bytes = bytes;
            _offset = offset;
            Tier = tier;
            Count = count;
        }

        public DifficultyTier Tier { get; }

        public int Count { get; }

        public Puzzle PuzzleAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Bank holds {Count} puzzles.");

            var start = _offset + index * Board.CellCount;
            var clues = new int[Board.CellCount];
            var solution = new int[Board.CellCount];

            // One byte per cell: the clue in the high nibble (0 means empty),
            // the solution digit in the low nibble.
            for (var cell = 0; cell < Board.CellCount; cell++)
            {
                var packed = _bytes[start + cell];
                clues[cell] = (packed >> 4) & 0xF;
                solution[cell] = packed & 0xF;
            }

            return new Puzzle(clues, solution);
        }
    }
}
