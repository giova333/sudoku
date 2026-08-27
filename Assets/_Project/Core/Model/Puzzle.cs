using System;

namespace Sudoku.Core.Model
{
    /// <summary>
    /// An immutable puzzle: the clues the player starts with, plus the unique
    /// solution. Shipping the solution alongside the clues is what makes
    /// solution-based mistake detection free at runtime.
    /// </summary>
    public sealed class Puzzle
    {
        readonly int[] _clues;
        readonly int[] _solution;

        public Puzzle(int[] clues, int[] solution)
        {
            if (clues == null) throw new ArgumentNullException(nameof(clues));
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            if (clues.Length != Board.CellCount)
                throw new ArgumentException($"Expected {Board.CellCount} clues, got {clues.Length}.", nameof(clues));
            if (solution.Length != Board.CellCount)
                throw new ArgumentException($"Expected {Board.CellCount} solution cells, got {solution.Length}.", nameof(solution));

            _clues = clues;
            _solution = solution;
        }

        public int ClueAt(int index) => _clues[index];

        public int SolutionAt(int index) => _solution[index];

        public bool IsGiven(int index) => _clues[index] != Board.Empty;

        /// <summary>
        /// Builds a puzzle from two 81-character strings where '1'-'9' are
        /// digits and any other character ('0', '.', '-') means empty.
        /// </summary>
        public static Puzzle FromStrings(string clues, string solution) =>
            new Puzzle(ParseGrid(clues, nameof(clues)), ParseGrid(solution, nameof(solution)));

        static int[] ParseGrid(string text, string paramName)
        {
            if (text == null) throw new ArgumentNullException(paramName);
            if (text.Length != Board.CellCount)
                throw new ArgumentException($"Expected {Board.CellCount} characters, got {text.Length}.", paramName);

            var grid = new int[Board.CellCount];
            for (var i = 0; i < Board.CellCount; i++)
            {
                var c = text[i];
                grid[i] = c >= '1' && c <= '9' ? c - '0' : Board.Empty;
            }
            return grid;
        }
    }
}
