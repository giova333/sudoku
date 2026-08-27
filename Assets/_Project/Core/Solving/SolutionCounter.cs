using Sudoku.Core.Model;

namespace Sudoku.Core.Solving
{
    /// <summary>
    /// Exhaustive backtracking search. Its only job is to answer "how many
    /// solutions does this grid have, up to a limit" - which is what makes a
    /// generated puzzle *proper*. It knows nothing about human technique; that
    /// is <see cref="TechniqueSolver"/>'s job.
    /// </summary>
    public static class SolutionCounter
    {
        const int AllDigits = 0x1FF; // bits 0..8 == digits 1..9

        /// <summary>
        /// Counts solutions, stopping once <paramref name="limit"/> is reached.
        /// Pass a limit of 2 to ask the only question that usually matters:
        /// is this puzzle uniquely solvable?
        /// </summary>
        public static int Count(int[] grid, ConstraintSet constraints, int limit)
        {
            var working = (int[])grid.Clone();
            if (!IsConsistent(working, constraints))
                return 0;

            var found = 0;
            Search(working, constraints, limit, ref found, null);
            return found;
        }

        /// <summary>
        /// Finds one solution, if any. Used where the caller already knows the
        /// grid is proper and simply wants the answer key.
        /// </summary>
        public static bool TrySolve(int[] grid, ConstraintSet constraints, out int[] solution)
        {
            solution = null;

            var working = (int[])grid.Clone();
            if (!IsConsistent(working, constraints))
                return false;

            var found = 0;
            int[] captured = null;
            Search(working, constraints, 1, ref found, g => captured = (int[])g.Clone());

            solution = captured;
            return found > 0;
        }

        /// <summary>
        /// Depth-first search that always branches on the most constrained empty
        /// cell. Choosing the fewest-candidates cell collapses the search space
        /// enormously versus scanning in index order.
        /// </summary>
        static void Search(int[] grid, ConstraintSet constraints, int limit, ref int found,
            System.Action<int[]> onSolution)
        {
            var bestIndex = -1;
            var bestMask = 0;
            var bestCount = int.MaxValue;

            for (var i = 0; i < Board.CellCount; i++)
            {
                if (grid[i] != Board.Empty)
                    continue;

                var mask = CandidatesAt(grid, constraints, i);
                var count = PopCount(mask);

                if (count == 0)
                    return; // dead end

                if (count < bestCount)
                {
                    bestCount = count;
                    bestIndex = i;
                    bestMask = mask;
                    if (count == 1)
                        break; // cannot do better
                }
            }

            if (bestIndex < 0)
            {
                found++;
                onSolution?.Invoke(grid);
                return;
            }

            for (var digit = 1; digit <= Board.Size; digit++)
            {
                if ((bestMask & (1 << (digit - 1))) == 0)
                    continue;

                grid[bestIndex] = digit;
                Search(grid, constraints, limit, ref found, onSolution);
                grid[bestIndex] = Board.Empty;

                if (found >= limit)
                    return;
            }
        }

        /// <summary>Digits that may legally go in a cell, as a 9-bit mask.</summary>
        public static int CandidatesAt(int[] grid, ConstraintSet constraints, int index)
        {
            var used = 0;
            foreach (var peer in constraints.PeersOf(index))
            {
                var value = grid[peer];
                if (value != Board.Empty)
                    used |= 1 << (value - 1);
            }
            return AllDigits & ~used;
        }

        /// <summary>True when no group already contains a repeated digit.</summary>
        public static bool IsConsistent(int[] grid, ConstraintSet constraints)
        {
            foreach (var group in constraints.Groups)
            {
                var seen = 0;
                foreach (var cell in group)
                {
                    var value = grid[cell];
                    if (value == Board.Empty)
                        continue;

                    var bit = 1 << (value - 1);
                    if ((seen & bit) != 0)
                        return false;
                    seen |= bit;
                }
            }
            return true;
        }

        static int PopCount(int mask)
        {
            var n = 0;
            while (mask != 0)
            {
                mask &= mask - 1;
                n++;
            }
            return n;
        }
    }
}
