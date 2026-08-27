using Sudoku.Core.Model;
using Sudoku.Core.Solving;

namespace Sudoku.Core.Generation
{
    /// <summary>
    /// Builds proper puzzles: a complete solution grid, then clues carved out of
    /// it while uniqueness is preserved at every step.
    ///
    /// This runs offline in the bake tool, never on a player's device, so it can
    /// afford to be thorough.
    /// </summary>
    public static class PuzzleGenerator
    {
        /// <summary>
        /// Generates a proper puzzle. The same seed always yields the same
        /// puzzle, which is what makes a baked bank reproducible.
        /// </summary>
        public static Puzzle Generate(int seed, ConstraintSet constraints)
        {
            return Generate(seed, constraints, symmetric: false);
        }

        /// <summary>
        /// When <paramref name="symmetric"/> is set, clues are removed in
        /// 180-degree rotational pairs, which is what gives commercial puzzle
        /// banks their tidy look. Symmetry constrains how hard a puzzle can get,
        /// so the hardest tiers relax it.
        /// </summary>
        public static Puzzle Generate(int seed, ConstraintSet constraints, bool symmetric)
        {
            var random = new DeterministicRandom(seed);
            var solution = BuildSolution(random, constraints);
            var clues = CarveClues(solution, constraints, random, symmetric);
            return new Puzzle(clues, solution);
        }

        /// <summary>
        /// Fills an empty grid by backtracking with the candidate order
        /// shuffled, so every seed lands on a different solution grid.
        /// </summary>
        public static int[] BuildSolution(DeterministicRandom random, ConstraintSet constraints)
        {
            var grid = new int[Board.CellCount];
            Fill(grid, constraints, random);
            return grid;
        }

        static bool Fill(int[] grid, ConstraintSet constraints, DeterministicRandom random)
        {
            var bestIndex = -1;
            var bestMask = 0;
            var bestCount = int.MaxValue;

            for (var i = 0; i < Board.CellCount; i++)
            {
                if (grid[i] != Board.Empty)
                    continue;

                var mask = SolutionCounter.CandidatesAt(grid, constraints, i);
                var count = CountBits(mask);
                if (count == 0)
                    return false;

                if (count < bestCount)
                {
                    bestCount = count;
                    bestIndex = i;
                    bestMask = mask;
                    if (count == 1)
                        break;
                }
            }

            if (bestIndex < 0)
                return true; // grid is full

            var digits = DigitsOf(bestMask);
            random.Shuffle(digits);

            foreach (var digit in digits)
            {
                grid[bestIndex] = digit;
                if (Fill(grid, constraints, random))
                    return true;
                grid[bestIndex] = Board.Empty;
            }

            return false;
        }

        /// <summary>
        /// Walks the cells in random order removing clues, keeping each removal
        /// only while the puzzle still has exactly one solution.
        /// </summary>
        static int[] CarveClues(int[] solution, ConstraintSet constraints, DeterministicRandom random, bool symmetric)
        {
            var clues = (int[])solution.Clone();

            var order = new int[Board.CellCount];
            for (var i = 0; i < Board.CellCount; i++) order[i] = i;
            random.Shuffle(order);

            foreach (var index in order)
            {
                if (clues[index] == Board.Empty)
                    continue;

                var partner = symmetric ? Board.CellCount - 1 - index : index;

                var removedIndex = clues[index];
                var removedPartner = clues[partner];
                clues[index] = Board.Empty;
                clues[partner] = Board.Empty;

                if (SolutionCounter.Count(clues, constraints, 2) != 1)
                {
                    clues[index] = removedIndex;
                    clues[partner] = removedPartner;
                }
            }

            return clues;
        }

        static int[] DigitsOf(int mask)
        {
            var count = CountBits(mask);
            var digits = new int[count];
            var n = 0;
            for (var d = 1; d <= Board.Size; d++)
                if ((mask & (1 << (d - 1))) != 0)
                    digits[n++] = d;
            return digits;
        }

        static int CountBits(int mask)
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
