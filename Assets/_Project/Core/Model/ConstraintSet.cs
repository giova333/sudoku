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
        GroupIntersection[] _intersections;

        public ConstraintSet(int[][] groups)
        {
            _groups = groups ?? throw new ArgumentNullException(nameof(groups));
            _peers = BuildPeers(groups);
        }

        /// <summary>
        /// Row groups, if this shape has rows. Populated for the classic board;
        /// exotic constraint sets may leave it empty, in which case techniques
        /// that reason about lines simply never fire.
        /// </summary>
        public int[][] Rows { get; private set; } = new int[0][];

        /// <summary>Column groups, if this shape has columns. See <see cref="Rows"/>.</summary>
        public int[][] Columns { get; private set; } = new int[0][];

        /// <summary>
        /// Every pair of groups sharing two or more cells, with those shared
        /// cells. This is all "locked candidates" needs: if a digit's only homes
        /// in group A fall inside the overlap with B, it can be struck from the
        /// rest of B. Expressing it this way keeps the technique correct for any
        /// constraint set rather than only for boxes and lines.
        /// </summary>
        public GroupIntersection[] Intersections =>
            _intersections ?? (_intersections = BuildIntersections(_groups));

        static GroupIntersection[] BuildIntersections(int[][] groups)
        {
            var result = new List<GroupIntersection>();

            for (var a = 0; a < groups.Length; a++)
            for (var b = 0; b < groups.Length; b++)
            {
                if (a == b) continue;

                var shared = new List<int>();
                var setB = new HashSet<int>(groups[b]);
                foreach (var cell in groups[a])
                    if (setB.Contains(cell))
                        shared.Add(cell);

                if (shared.Count >= 2 && shared.Count < groups[a].Length)
                    result.Add(new GroupIntersection(groups[a], groups[b], shared.ToArray()));
            }

            return result.ToArray();
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

            var set = new ConstraintSet(groups.ToArray());
            set.Rows = groups.GetRange(0, Board.Size).ToArray();
            set.Columns = groups.GetRange(Board.Size, Board.Size).ToArray();
            return set;
        }
    }
}
