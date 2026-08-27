using System.Diagnostics;
using NUnit.Framework;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Generation;
using Sudoku.Core.Model;
using Sudoku.Core.Solving;

namespace Sudoku.Core.Tests.Generation
{
    [TestFixture]
    public class GradedGenerationTests
    {
        static int[] CluesOf(Puzzle puzzle)
        {
            var clues = new int[Board.CellCount];
            for (var i = 0; i < Board.CellCount; i++) clues[i] = puzzle.ClueAt(i);
            return clues;
        }

        [Test]
        public void A_puzzle_generated_for_a_tier_actually_grades_to_that_tier()
        {
            foreach (DifficultyTier tier in System.Enum.GetValues(typeof(DifficultyTier)))
            {
                var puzzle = PuzzleGenerator.GenerateForTier(
                    seed: 1000 + (int)tier, tier: tier,
                    profile: DifficultyProfile.Default,
                    constraints: ConstraintSet.Classic,
                    maxAttempts: 200);

                Assert.That(puzzle, Is.Not.Null, $"could not generate a {tier} puzzle");

                var grade = PuzzleGrader.Grade(CluesOf(puzzle), ConstraintSet.Classic, DifficultyProfile.Default);
                Assert.That(grade, Is.EqualTo(tier));
            }
        }

        [Test]
        public void Every_tier_targeted_puzzle_is_still_proper()
        {
            foreach (DifficultyTier tier in System.Enum.GetValues(typeof(DifficultyTier)))
            {
                var puzzle = PuzzleGenerator.GenerateForTier(
                    2000 + (int)tier, tier, DifficultyProfile.Default, ConstraintSet.Classic, 200);

                Assert.That(SolutionCounter.Count(CluesOf(puzzle), ConstraintSet.Classic, 2), Is.EqualTo(1),
                    $"{tier} puzzle is not uniquely solvable");
            }
        }

        [Test]
        public void Tiers_that_ask_for_symmetry_get_it()
        {
            foreach (var rule in DifficultyProfile.Default.Tiers)
            {
                if (!rule.Symmetric) continue;

                var puzzle = PuzzleGenerator.GenerateForTier(
                    3000 + (int)rule.Tier, rule.Tier, DifficultyProfile.Default, ConstraintSet.Classic, 200);

                for (var i = 0; i < Board.CellCount; i++)
                {
                    var mirror = Board.CellCount - 1 - i;
                    Assert.That(puzzle.IsGiven(i), Is.EqualTo(puzzle.IsGiven(mirror)),
                        $"{rule.Tier}: cell {i} and its 180-degree mirror {mirror} disagree");
                }
            }
        }

        [Test]
        public void The_same_seed_and_tier_generate_the_same_puzzle()
        {
            var a = PuzzleGenerator.GenerateForTier(555, DifficultyTier.Hard, DifficultyProfile.Default, ConstraintSet.Classic, 200);
            var b = PuzzleGenerator.GenerateForTier(555, DifficultyTier.Hard, DifficultyProfile.Default, ConstraintSet.Classic, 200);

            Assert.That(GridParser.ToText(CluesOf(b)), Is.EqualTo(GridParser.ToText(CluesOf(a))));
        }

        [Test]
        public void Generation_is_fast_enough_to_bake_a_bank()
        {
            // The bake produces 5 tiers x 2000 puzzles. If a single puzzle costs
            // more than a second the bake stops being a coffee break and starts
            // being an overnight job, so this is worth knowing about early.
            var watch = Stopwatch.StartNew();
            var produced = 0;

            foreach (DifficultyTier tier in System.Enum.GetValues(typeof(DifficultyTier)))
            {
                for (var n = 0; n < 3; n++)
                {
                    var puzzle = PuzzleGenerator.GenerateForTier(
                        7000 + (int)tier * 100 + n, tier, DifficultyProfile.Default, ConstraintSet.Classic, 200);
                    if (puzzle != null) produced++;
                }
            }

            watch.Stop();
            var perPuzzle = watch.ElapsedMilliseconds / (double)produced;
            TestContext.WriteLine($"generated {produced} graded puzzles, {perPuzzle:F0} ms each");

            Assert.That(produced, Is.EqualTo(15));
            Assert.That(perPuzzle, Is.LessThan(2000), "graded generation is too slow to bake a bank");
        }
    }
}
