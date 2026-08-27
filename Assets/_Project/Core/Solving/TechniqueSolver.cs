using System;
using System.Collections.Generic;
using Sudoku.Core.Model;

namespace Sudoku.Core.Solving
{
    /// <summary>
    /// Solves the way a person does, by applying named techniques in escalating
    /// order and reporting which one fired. One component serves three jobs:
    /// it grades a puzzle's difficulty, it powers the in-game hint, and it
    /// validates the puzzle bank at bake time.
    /// </summary>
    public static class TechniqueSolver
    {
        const int AllDigits = 0x1FF;

        /// <summary>
        /// The easiest deduction available on this board, or null when no known
        /// technique applies (either the grid is finished, or it is harder than
        /// this solver can see).
        /// </summary>
        public static SolveStep NextStep(int[] grid, ConstraintSet constraints)
        {
            var candidates = BuildCandidates(grid, constraints);
            return NextStep(grid, candidates, constraints);
        }

        /// <summary>
        /// Applies techniques until the grid is finished or nothing else can be
        /// deduced. The resulting report is what grading reads.
        /// </summary>
        public static SolveReport Solve(int[] grid, ConstraintSet constraints)
        {
            var working = (int[])grid.Clone();
            var counts = new int[Enum.GetValues(typeof(Technique)).Length];
            var totalSteps = 0;

            while (true)
            {
                var candidates = BuildCandidates(working, constraints);
                var step = NextStep(working, candidates, constraints);
                if (step == null)
                    break;

                counts[(int)step.Technique]++;
                totalSteps++;

                if (step.IsPlacement)
                {
                    working[step.CellIndex] = step.Digit;
                }
                else
                {
                    // Elimination steps narrow candidates rather than filling a
                    // cell; applying them is what unlocks the next single.
                    foreach (var e in step.Eliminations)
                        candidates[e.Cell] &= ~(1 << (e.Digit - 1));

                    var follow = ApplyEliminationsUntilProgress(working, candidates, constraints, counts, ref totalSteps);
                    if (!follow)
                        break;
                }
            }

            var solved = IsComplete(working);
            return new SolveReport(solved, working, counts, totalSteps);
        }

        /// <summary>
        /// After an elimination, keep working the narrowed candidate grid until
        /// a digit can actually be placed. Without this an elimination-only
        /// technique would be recomputed from scratch and lost.
        /// </summary>
        static bool ApplyEliminationsUntilProgress(int[] grid, int[] candidates, ConstraintSet constraints,
            int[] counts, ref int totalSteps)
        {
            while (true)
            {
                var step = NextStep(grid, candidates, constraints);
                if (step == null)
                    return false;

                counts[(int)step.Technique]++;
                totalSteps++;

                if (step.IsPlacement)
                {
                    grid[step.CellIndex] = step.Digit;
                    return true;
                }

                foreach (var e in step.Eliminations)
                {
                    var before = candidates[e.Cell];
                    candidates[e.Cell] &= ~(1 << (e.Digit - 1));
                    if (before == candidates[e.Cell])
                        return false; // no progress; give up rather than spin
                }
            }
        }

        static bool IsComplete(int[] grid)
        {
            for (var i = 0; i < Board.CellCount; i++)
                if (grid[i] == Board.Empty)
                    return false;
            return true;
        }

        internal static SolveStep NextStep(int[] grid, int[] candidates, ConstraintSet constraints)
        {
            // Techniques are tried easiest-first, so a step is always reported
            // as the simplest way to see it.
            return FindNakedSingle(grid, candidates, constraints)
                ?? FindHiddenSingle(grid, candidates, constraints);
        }

        static SolveStep FindNakedSingle(int[] grid, int[] candidates, ConstraintSet constraints)
        {
            for (var i = 0; i < Board.CellCount; i++)
            {
                if (grid[i] != Board.Empty)
                    continue;

                var mask = candidates[i];
                if (PopCount(mask) != 1)
                    continue;

                return new SolveStep(Technique.NakedSingle, i, DigitOf(mask),
                    FilledPeersOf(grid, constraints, i));
            }
            return null;
        }

        /// <summary>
        /// A digit that fits in only one cell of a group belongs there, even
        /// when that cell has other candidates of its own.
        /// </summary>
        static SolveStep FindHiddenSingle(int[] grid, int[] candidates, ConstraintSet constraints)
        {
            foreach (var group in constraints.Groups)
            {
                for (var digit = 1; digit <= Board.Size; digit++)
                {
                    var bit = 1 << (digit - 1);
                    var home = -1;
                    var alreadyPlaced = false;

                    foreach (var cell in group)
                    {
                        if (grid[cell] == digit) { alreadyPlaced = true; break; }
                        if (grid[cell] != Board.Empty) continue;
                        if ((candidates[cell] & bit) == 0) continue;

                        if (home >= 0) { home = -2; break; } // more than one home
                        home = cell;
                    }

                    if (alreadyPlaced || home < 0)
                        continue;

                    return new SolveStep(Technique.HiddenSingle, home, digit, (int[])group.Clone());
                }
            }
            return null;
        }

        /// <summary>The filled peers that rule out every other digit for a cell.</summary>
        static int[] FilledPeersOf(int[] grid, ConstraintSet constraints, int index)
        {
            var reason = new List<int>();
            foreach (var peer in constraints.PeersOf(index))
                if (grid[peer] != Board.Empty)
                    reason.Add(peer);
            return reason.ToArray();
        }

        /// <summary>Candidate masks for every cell; filled cells get 0.</summary>
        public static int[] BuildCandidates(int[] grid, ConstraintSet constraints)
        {
            var candidates = new int[Board.CellCount];
            for (var i = 0; i < Board.CellCount; i++)
            {
                if (grid[i] != Board.Empty)
                    continue;

                var used = 0;
                foreach (var peer in constraints.PeersOf(i))
                {
                    var value = grid[peer];
                    if (value != Board.Empty)
                        used |= 1 << (value - 1);
                }
                candidates[i] = AllDigits & ~used;
            }
            return candidates;
        }

        /// <summary>How many candidates a mask holds. Exposed for tests.</summary>
        public static int PopCountOf(int mask) => PopCount(mask);

        internal static int PopCount(int mask)
        {
            var n = 0;
            while (mask != 0)
            {
                mask &= mask - 1;
                n++;
            }
            return n;
        }

        internal static int DigitOf(int singleBitMask)
        {
            for (var d = 1; d <= Board.Size; d++)
                if (singleBitMask == 1 << (d - 1))
                    return d;
            return Board.Empty;
        }
    }
}
