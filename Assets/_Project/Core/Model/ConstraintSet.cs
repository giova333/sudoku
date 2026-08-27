using System;
using System.Collections.Generic;

namespace Sudoku.Core.Model
{
    /// <summary>
    /// The rule shape of a board, expressed as groups of cells that must each
    /// contain distinct values. Classic Sudoku is 27 such groups (9 rows,
    /// 9 columns, 9 boxes); variants like Sudoku-X or Jigsaw are the same board
    /// with a different group list, which is why the rules are data here rather
    /// than row/column/box arithmetic baked into the solver.
    /// </summary>
    public sealed class ConstraintSet
    {
        readonly int[][] _groups;
        readonly int[][] _peers;

        public ConstraintSet(int[][] groups)
        {
            _groups = groups ?? throw new ArgumentNullException(nameof(groups));
            _peers = BuildPeers(groups);
        }

        /// <summary>All groups that must contain distinct values.</summary>
        public IReadOnlyList<int[]> Groups => _groups;

        /// <summary>
        /// Every cell that shares at least one group with the given cell,
        /// excluding the cell itself. Precomputed because solving and note
        /// bookkeeping walk this constantly.
        /// </summary>
        public int[] PeersOf(int index) => _peers[index];

        static int[][] BuildPeers(int[][] groups)
        {
            var sets = new HashSet<int>[Board.CellCount];
            for (var i = 0; i < Board.CellCount; i++)
                sets[i] = new HashSet<int>();

            foreach (var group in groups)
                foreach (var cell in group)
                    foreach (var other in group)
                        if (other != cell)
                            sets[cell].Add(other);

            var peers = new int[Board.CellCount][];
            for (var i = 0; i < Board.CellCount; i++)
            {
                peers[i] = new int[sets[i].Count];
                sets[i].CopyTo(peers[i]);
                Array.Sort(peers[i]);
            }
            return peers;
        }

        static ConstraintSet _classic;

        /// <summary>Standard 9x9 Sudoku: 9 rows, 9 columns, 9 boxes.</summary>
        public static ConstraintSet Classic => _classic ?? (_classic = BuildClassic());

        static ConstraintSet BuildClassic()
        {
            var groups = new List<int[]>(27);

            for (var row = 0; row < Board.Size; row++)
            {
                var group = new int[Board.Size];
                for (var col = 0; col < Board.Size; col++)
                    group[col] = row * Board.Size + col;
                groups.Add(group);
            }

            for (var col = 0; col < Board.Size; col++)
            {
                var group = new int[Board.Size];
                for (var row = 0; row < Board.Size; row++)
                    group[row] = row * Board.Size + col;
                groups.Add(group);
            }

            for (var boxRow = 0; boxRow < 3; boxRow++)
            for (var boxCol = 0; boxCol < 3; boxCol++)
            {
                var group = new int[Board.Size];
                var n = 0;
                for (var r = 0; r < 3; r++)
                for (var c = 0; c < 3; c++)
                    group[n++] = (boxRow * 3 + r) * Board.Size + boxCol * 3 + c;
                groups.Add(group);
            }

            return new ConstraintSet(groups.ToArray());
        }
    }
}
