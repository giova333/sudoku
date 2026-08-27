using NUnit.Framework;
using Sudoku.Core.Content;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using Sudoku.Core.Solving;

namespace Sudoku.Core.Tests.Generation
{
    [TestFixture]
    public class BankBakerTests
    {
        static BakeResult Bake(DifficultyTier tier, int count, int seed = 1) =>
            BankBaker.Bake(new BakeRequest(tier, count, seed), DifficultyProfile.Default, ConstraintSet.Classic);

        [Test]
        public void A_bake_produces_the_requested_number_of_puzzles()
        {
            var result = Bake(DifficultyTier.Medium, 12);

            Assert.That(result.Produced, Is.EqualTo(12));
            Assert.That(result.Puzzles.Length, Is.EqualTo(12));
        }

        [Test]
        public void Every_baked_puzzle_grades_to_the_banks_tier()
        {
            foreach (DifficultyTier tier in System.Enum.GetValues(typeof(DifficultyTier)))
            {
                var result = Bake(tier, 5, seed: 20 + (int)tier);

                foreach (var puzzle in result.Puzzles)
                {
                    var clues = new int[Board.CellCount];
                    for (var i = 0; i < Board.CellCount; i++) clues[i] = puzzle.ClueAt(i);

                    Assert.That(PuzzleGrader.Grade(clues, ConstraintSet.Classic, DifficultyProfile.Default),
                        Is.EqualTo(tier));
                }
            }
        }

        [Test]
        public void Every_baked_puzzle_is_uniquely_solvable()
        {
            var result = Bake(DifficultyTier.Hard, 10, seed: 5);

            foreach (var puzzle in result.Puzzles)
            {
                var clues = new int[Board.CellCount];
                for (var i = 0; i < Board.CellCount; i++) clues[i] = puzzle.ClueAt(i);

                Assert.That(SolutionCounter.Count(clues, ConstraintSet.Classic, 2), Is.EqualTo(1));
            }
        }

        [Test]
        public void A_bank_never_contains_the_same_puzzle_twice()
        {
            var result = Bake(DifficultyTier.Easy, 25, seed: 3);
            var seen = new System.Collections.Generic.HashSet<string>();

            foreach (var puzzle in result.Puzzles)
            {
                var clues = new int[Board.CellCount];
                for (var i = 0; i < Board.CellCount; i++) clues[i] = puzzle.ClueAt(i);

                Assert.That(seen.Add(GridParser.ToText(clues)), Is.True, "duplicate puzzle in bank");
            }
        }

        [Test]
        public void The_same_seed_bakes_a_byte_identical_bank()
        {
            var first = Bake(DifficultyTier.Medium, 8, seed: 4242);
            var second = Bake(DifficultyTier.Medium, 8, seed: 4242);

            var a = PuzzleBankSerializer.Write(DifficultyTier.Medium, first.Puzzles);
            var b = PuzzleBankSerializer.Write(DifficultyTier.Medium, second.Puzzles);

            Assert.That(b, Is.EqualTo(a), "a re-bake must reproduce the bank exactly");
        }

        [Test]
        public void The_report_accounts_for_everything_the_bake_did()
        {
            var result = Bake(DifficultyTier.Expert, 6, seed: 9);

            Assert.That(result.Requested, Is.EqualTo(6));
            Assert.That(result.Produced, Is.EqualTo(6));
            Assert.That(result.AttemptsUsed, Is.GreaterThanOrEqualTo(result.Produced));
            Assert.That(result.Tier, Is.EqualTo(DifficultyTier.Expert));
        }

        [Test]
        public void A_bake_that_cannot_reach_its_target_says_so_rather_than_hanging()
        {
            // A deliberately impossible budget: the request must come back
            // short and honest, never silently truncated or spinning forever.
            var result = BankBaker.Bake(
                new BakeRequest(DifficultyTier.Master, count: 50, seed: 1) { MaxTotalAttempts = 5 },
                DifficultyProfile.Default, ConstraintSet.Classic);

            Assert.That(result.Produced, Is.LessThan(50));
            Assert.That(result.FellShort, Is.True);
        }
    }
}
