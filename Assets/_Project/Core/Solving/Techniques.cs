using System.Collections.Generic;
using Sudoku.Core.Model;

namespace Sudoku.Core.Solving
{
    /// <summary>
    /// The elimination techniques - deductions that narrow candidates rather
    /// than placing a digit. Each returns a step only when it removes at least
    /// one candidate that is still present, so the solver can never spin on a
    /// technique that reports progress it did not make.
    /// </summary>
    internal static class Techniques
    {
        /// <summary>
        /// If a digit's only homes inside one group all fall within its overlap
        /// with another group, the digit can be struck from the rest of that
        /// second group.
        /// </summary>
        public static SolveStep LockedCandidates(int[] grid, int[] candidates, ConstraintSet constraints)
        {
            foreach (var pair in constraints.Intersections)
            {
                for (var digit = 1; digit <= Board.Size; digit++)
                {
                    var bit = 1 << (digit - 1);

                    if (GroupHolds(grid, pair.Source, digit) || GroupHolds(grid, pair.Target, digit))
                        continue;

                    // Every home for this digit in Source must be shared with Target.
                    var confined = true;
                    var anyHome = false;
                    foreach (var cell in pair.Source)
                    {
                        if (grid[cell] != Board.Empty || (candidates[cell] & bit) == 0)
                            continue;

                        anyHome = true;
                        if (!Contains(pair.Shared, cell)) { confined = false; break; }
                    }
                    if (!anyHome || !confined)
                        continue;

                    var eliminations = new List<(int, int)>();
                    foreach (var cell in pair.Target)
                    {
                        if (Contains(pair.Shared, cell)) continue;
                        if (grid[cell] != Board.Empty) continue;
                        if ((candidates[cell] & bit) != 0)
                            eliminations.Add((cell, digit));
                    }

                    if (eliminations.Count == 0)
                        continue;

                    return Elimination(Technique.LockedCandidates, digit, pair.Shared, eliminations);
                }
            }
            return null;
        }

        /// <summary>
        /// k cells in a group whose candidates between them are exactly k
        /// digits: those digits belong to those cells, so no other cell in the
        /// group can hold them.
        /// </summary>
        public static SolveStep NakedSubset(int[] grid, int[] candidates, ConstraintSet constraints, int size, Technique technique)
        {
            foreach (var group in constraints.Groups)
            {
                var open = OpenCells(grid, group);
                if (open.Count <= size)
                    continue;

                var combo = new int[size];
                foreach (var indices in Combinations(open.Count, size, combo))
                {
                    var union = 0;
                    for (var i = 0; i < size; i++)
                        union |= candidates[open[indices[i]]];

                    if (TechniqueSolver.PopCount(union) != size)
                        continue;

                    var eliminations = new List<(int, int)>();
                    for (var i = 0; i < open.Count; i++)
                    {
                        if (IsChosen(indices, i)) continue;

                        var cell = open[i];
                        var overlap = candidates[cell] & union;
                        for (var digit = 1; digit <= Board.Size; digit++)
                            if ((overlap & (1 << (digit - 1))) != 0)
                                eliminations.Add((cell, digit));
                    }

                    if (eliminations.Count == 0)
                        continue;

                    var reason = new int[size];
                    for (var i = 0; i < size; i++) reason[i] = open[indices[i]];
                    return Elimination(technique, Board.Empty, reason, eliminations);
                }
            }
            return null;
        }

        /// <summary>
        /// k digits in a group that between them can only go in k cells: those
        /// cells hold those digits, so every other candidate in them is out.
        /// </summary>
        public static SolveStep HiddenSubset(int[] grid, int[] candidates, ConstraintSet constraints, int size, Technique technique)
        {
            foreach (var group in constraints.Groups)
            {
                var missing = new List<int>();
                for (var digit = 1; digit <= Board.Size; digit++)
                    if (!GroupHolds(grid, group, digit))
                        missing.Add(digit);

                if (missing.Count <= size)
                    continue;

                var combo = new int[size];
                foreach (var indices in Combinations(missing.Count, size, combo))
                {
                    var homes = new List<int>();
                    var digitMask = 0;

                    for (var i = 0; i < size; i++)
                    {
                        var digit = missing[indices[i]];
                        digitMask |= 1 << (digit - 1);

                        foreach (var cell in group)
                        {
                            if (grid[cell] != Board.Empty) continue;
                            if ((candidates[cell] & (1 << (digit - 1))) == 0) continue;
                            if (!homes.Contains(cell)) homes.Add(cell);
                        }
                    }

                    if (homes.Count != size)
                        continue;

                    var eliminations = new List<(int, int)>();
                    foreach (var cell in homes)
                    {
                        var extra = candidates[cell] & ~digitMask;
                        for (var digit = 1; digit <= Board.Size; digit++)
                            if ((extra & (1 << (digit - 1))) != 0)
                                eliminations.Add((cell, digit));
                    }

                    if (eliminations.Count == 0)
                        continue;

                    return Elimination(technique, Board.Empty, homes.ToArray(), eliminations);
                }
            }
            return null;
        }

        /// <summary>
        /// A digit confined to the same two columns across two rows (or the
        /// reverse) forms a rectangle: in those two columns the digit must lie
        /// on one of those rows, so it is out everywhere else in them.
        /// </summary>
        public static SolveStep XWing(int[] grid, int[] candidates, ConstraintSet constraints)
        {
            return XWingOver(grid, candidates, constraints.Rows, constraints.Columns)
                ?? XWingOver(grid, candidates, constraints.Columns, constraints.Rows);
        }

        static SolveStep XWingOver(int[] grid, int[] candidates, int[][] lines, int[][] crossLines)
        {
            if (lines.Length == 0 || crossLines.Length == 0)
                return null;

            for (var digit = 1; digit <= Board.Size; digit++)
            {
                var bit = 1 << (digit - 1);

                for (var a = 0; a < lines.Length; a++)
                {
                    var homesA = HomesFor(grid, candidates, lines[a], bit);
                    if (homesA.Count != 2) continue;

                    for (var b = a + 1; b < lines.Length; b++)
                    {
                        var homesB = HomesFor(grid, candidates, lines[b], bit);
                        if (homesB.Count != 2) continue;

                        var crossA0 = IndexOfLineContaining(crossLines, homesA[0]);
                        var crossA1 = IndexOfLineContaining(crossLines, homesA[1]);
                        var crossB0 = IndexOfLineContaining(crossLines, homesB[0]);
                        var crossB1 = IndexOfLineContaining(crossLines, homesB[1]);

                        if (crossA0 != crossB0 || crossA1 != crossB1)
                            continue;

                        var eliminations = new List<(int, int)>();
                        foreach (var crossIndex in new[] { crossA0, crossA1 })
                        {
                            foreach (var cell in crossLines[crossIndex])
                            {
                                if (grid[cell] != Board.Empty) continue;
                                if ((candidates[cell] & bit) == 0) continue;
                                if (cell == homesA[0] || cell == homesA[1] ||
                                    cell == homesB[0] || cell == homesB[1]) continue;

                                eliminations.Add((cell, digit));
                            }
                        }

                        if (eliminations.Count == 0)
                            continue;

                        var reason = new[] { homesA[0], homesA[1], homesB[0], homesB[1] };
                        return Elimination(Technique.XWing, digit, reason, eliminations);
                    }
                }
            }
            return null;
        }

        static List<int> HomesFor(int[] grid, int[] candidates, int[] group, int bit)
        {
            var homes = new List<int>();
            foreach (var cell in group)
            {
                if (grid[cell] != Board.Empty) continue;
                if ((candidates[cell] & bit) != 0) homes.Add(cell);
            }
            return homes;
        }

        static int IndexOfLineContaining(int[][] lines, int cell)
        {
            for (var i = 0; i < lines.Length; i++)
                if (Contains(lines[i], cell))
                    return i;
            return -1;
        }

        static SolveStep Elimination(Technique technique, int digit, int[] reason, List<(int, int)> eliminations)
        {
            var step = new SolveStep(technique, -1, digit, reason)
            {
                Eliminations = eliminations.ToArray()
            };
            return step;
        }

        static List<int> OpenCells(int[] grid, int[] group)
        {
            var open = new List<int>();
            foreach (var cell in group)
                if (grid[cell] == Board.Empty)
                    open.Add(cell);
            return open;
        }

        static bool GroupHolds(int[] grid, int[] group, int digit)
        {
            foreach (var cell in group)
                if (grid[cell] == digit)
                    return true;
            return false;
        }

        static bool Contains(int[] items, int value)
        {
            foreach (var item in items)
                if (item == value)
                    return true;
            return false;
        }

        static bool IsChosen(int[] indices, int i)
        {
            foreach (var chosen in indices)
                if (chosen == i)
                    return true;
            return false;
        }

        /// <summary>Index combinations of the given size, reusing one buffer.</summary>
        static IEnumerable<int[]> Combinations(int n, int k, int[] buffer)
        {
            if (k > n) yield break;

            for (var i = 0; i < k; i++) buffer[i] = i;

            while (true)
            {
                yield return buffer;

                var pos = k - 1;
                while (pos >= 0 && buffer[pos] == n - k + pos) pos--;
                if (pos < 0) yield break;

                buffer[pos]++;
                for (var i = pos + 1; i < k; i++) buffer[i] = buffer[i - 1] + 1;
            }
        }
    }
}
