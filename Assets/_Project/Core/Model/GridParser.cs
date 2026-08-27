using System;
using System.Text;

namespace Sudoku.Core.Model
{
    /// <summary>
    /// Converts between an 81-cell grid and its 81-character text form, where
    /// '1'-'9' are digits and any other character means empty.
    /// </summary>
    public static class GridParser
    {
        public static int[] Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length != Board.CellCount)
                throw new ArgumentException($"Expected {Board.CellCount} characters, got {text.Length}.", nameof(text));

            var grid = new int[Board.CellCount];
            for (var i = 0; i < Board.CellCount; i++)
            {
                var c = text[i];
                grid[i] = c >= '1' && c <= '9' ? c - '0' : Board.Empty;
            }
            return grid;
        }

        public static string ToText(int[] grid)
        {
            var sb = new StringBuilder(Board.CellCount);
            for (var i = 0; i < Board.CellCount; i++)
                sb.Append(grid[i] == Board.Empty ? '0' : (char)('0' + grid[i]));
            return sb.ToString();
        }
    }
}
