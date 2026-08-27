using NUnit.Framework;
using Sudoku.Core.Model;
using Sudoku.Core.Solving;

namespace Sudoku.Core.Tests.Generation
{
    /// <summary>
    /// Generation is random, so it is tested as invariants over volume rather
    /// than as fixed expectations. A fixed seed makes any failure reproducible.
    /// </summary>
    [TestFixture]
    public class PuzzleGeneratorTests
    {
        static int[] CluesOf(Puzzle puzzle)
        {
            var clues = new int[Board.CellCount];
            for (var i = 0; i < Board.CellCount; i++) clues[i] = puzzle.ClueAt(i);
            return clues;
        }

        static int[] SolutionOf(Puzzle puzzle)
        {
            var solution = new int[Board.CellCount];
            for (var i = 0; i < Board.CellCount; i++) solution[i] = puzzle.SolutionAt(i);
            return solution;
        }

        [Test]
        public void Every_generated_puzzle_has_exactly_one_solution()
        {
            for (var seed = 1; seed <= 40; seed++)
            {
                var puzzle = Sudoku.Core.Generation.PuzzleGenerator.Generate(seed, ConstraintSet.Classic);

                var count = SolutionCounter.Count(CluesOf(puzzle), ConstraintSet.Classic, 2);
                Assert.That(count, Is.EqualTo(1), $"seed {seed} produced an improper puzzle");
            }
        }

        [Test]
        public void A_generated_puzzles_solution_is_a_complete_valid_grid()
        {
            for (var seed = 1; seed <= 20; seed++)
            {
                var puzzle = Sudoku.Core.Generation.PuzzleGenerator.Generate(seed, ConstraintSet.Classic);
                var solution = SolutionOf(puzzle);

                foreach (var value in solution)
                    Assert.That(value, Is.InRange(1, 9), $"seed {seed} left a hole in the solution");
                Assert.That(SolutionCounter.IsConsistent(solution, ConstraintSet.Classic), Is.True,
                    $"seed {seed} produced a solution that breaks the rules");
            }
        }

        [Test]
        public void Every_clue_agrees_with_the_solution()
        {
            for (var seed = 1; seed <= 20; seed++)
            {
                var puzzle = Sudoku.Core.Generation.PuzzleGenerator.Generate(seed, ConstraintSet.Classic);

                for (var i = 0; i < Board.CellCount; i++)
                    if (puzzle.IsGiven(i))
                        Assert.That(puzzle.ClueAt(i), Is.EqualTo(puzzle.SolutionAt(i)),
                            $"seed {seed} cell {i}");
            }
        }

        [Test]
        public void The_same_seed_generates_the_same_puzzle()
        {
            var a = Sudoku.Core.Generation.PuzzleGenerator.Generate(4242, ConstraintSet.Classic);
            var b = Sudoku.Core.Generation.PuzzleGenerator.Generate(4242, ConstraintSet.Classic);

            Assert.That(GridParser.ToText(CluesOf(b)), Is.EqualTo(GridParser.ToText(CluesOf(a))));
            Assert.That(GridParser.ToText(SolutionOf(b)), Is.EqualTo(GridParser.ToText(SolutionOf(a))));
        }

        [Test]
        public void Different_seeds_generate_different_puzzles()
        {
            var seen = new System.Collections.Generic.HashSet<string>();

            for (var seed = 1; seed <= 25; seed++)
            {
                var puzzle = Sudoku.Core.Generation.PuzzleGenerator.Generate(seed, ConstraintSet.Classic);
                seen.Add(GridParser.ToText(CluesOf(puzzle)));
            }

            Assert.That(seen.Count, Is.EqualTo(25), "seeds should not collide");
        }

        [Test]
        public void A_generated_puzzle_leaves_the_player_something_to_do()
        {
            for (var seed = 1; seed <= 20; seed++)
            {
                var puzzle = Sudoku.Core.Generation.PuzzleGenerator.Generate(seed, ConstraintSet.Classic);

                var clues = 0;
                for (var i = 0; i < Board.CellCount; i++)
                    if (puzzle.IsGiven(i)) clues++;

                // A proper Sudoku needs at least 17 clues, and a puzzle that is
                // nearly filled in is not a puzzle.
                Assert.That(clues, Is.InRange(17, 60), $"seed {seed} gave {clues} clues");
            }
        }

        [Test]
        public void Removing_any_further_clue_is_not_required_but_uniqueness_is()
        {
            // Guards the one property that is non-negotiable: a player must
            // never be punished for a valid deduction.
            var puzzle = Sudoku.Core.Generation.PuzzleGenerator.Generate(777, ConstraintSet.Classic);

            Assert.That(SolutionCounter.Count(CluesOf(puzzle), ConstraintSet.Classic, 2), Is.EqualTo(1));
        }
    }
}
