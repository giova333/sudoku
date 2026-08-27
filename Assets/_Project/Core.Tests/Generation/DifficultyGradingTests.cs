using NUnit.Framework;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using Sudoku.Core.Solving;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Generation
{
    [TestFixture]
    public class DifficultyGradingTests
    {
        static int[] Grid(string text) => GridParser.Parse(text);

        [Test]
        public void A_famously_hard_puzzle_grades_at_the_top_tier()
        {
            var grade = PuzzleGrader.Grade(
                Grid(KnownPuzzles.AiEscargot), ConstraintSet.Classic, DifficultyProfile.Default);

            Assert.That(grade, Is.EqualTo(DifficultyTier.Master));
        }

        [Test]
        public void A_textbook_puzzle_grades_near_the_bottom()
        {
            var grade = PuzzleGrader.Grade(
                Grid(KnownPuzzles.ClassicClues), ConstraintSet.Classic, DifficultyProfile.Default);

            Assert.That(grade, Is.LessThanOrEqualTo(DifficultyTier.Medium),
                "the canonical textbook example should not read as hard");
        }

        [Test]
        public void Grading_the_same_puzzle_twice_gives_the_same_answer()
        {
            var grid = Grid(KnownPuzzles.ClassicClues);

            var first = PuzzleGrader.Grade(grid, ConstraintSet.Classic, DifficultyProfile.Default);
            var second = PuzzleGrader.Grade(grid, ConstraintSet.Classic, DifficultyProfile.Default);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Grading_does_not_disturb_the_grid_it_is_given()
        {
            var grid = Grid(KnownPuzzles.ClassicClues);

            PuzzleGrader.Grade(grid, ConstraintSet.Classic, DifficultyProfile.Default);

            Assert.That(GridParser.ToText(grid), Is.EqualTo(KnownPuzzles.ClassicClues));
        }

        [Test]
        public void Thresholds_are_data_so_moving_them_moves_the_grade()
        {
            // Find a puzzle the default profile calls Medium - i.e. one that
            // needs hidden singles but nothing harder.
            int[] mediumPuzzle = null;
            for (var seed = 1; seed <= 60 && mediumPuzzle == null; seed++)
            {
                var puzzle = Sudoku.Core.Generation.PuzzleGenerator.Generate(seed, ConstraintSet.Classic);
                var clues = new int[Board.CellCount];
                for (var i = 0; i < Board.CellCount; i++) clues[i] = puzzle.ClueAt(i);

                if (PuzzleGrader.Grade(clues, ConstraintSet.Classic, DifficultyProfile.Default) == DifficultyTier.Medium)
                    mediumPuzzle = clues;
            }
            Assert.That(mediumPuzzle, Is.Not.Null, "expected some generated puzzle to grade Medium");

            // Under a profile where no tier tolerates anything above a naked
            // single, that same puzzle must fall to the top tier. Nothing about
            // the puzzle changed - only the data.
            var strict = new DifficultyProfile(new[]
            {
                new TierRule(DifficultyTier.Easy, Technique.NakedSingle, symmetric: true),
                new TierRule(DifficultyTier.Medium, Technique.NakedSingle, symmetric: true),
                new TierRule(DifficultyTier.Hard, Technique.NakedSingle, symmetric: true),
                new TierRule(DifficultyTier.Expert, Technique.NakedSingle, symmetric: false),
                new TierRule(DifficultyTier.Master, Technique.NakedSingle, symmetric: false),
            });

            Assert.That(PuzzleGrader.Grade(mediumPuzzle, ConstraintSet.Classic, strict),
                Is.EqualTo(DifficultyTier.Master));
        }

        [Test]
        public void The_default_profile_covers_every_tier_exactly_once()
        {
            var seen = new System.Collections.Generic.HashSet<DifficultyTier>();
            foreach (var tier in DifficultyProfile.Default.Tiers)
                Assert.That(seen.Add(tier.Tier), Is.True, $"{tier.Tier} listed twice");

            foreach (DifficultyTier tier in System.Enum.GetValues(typeof(DifficultyTier)))
                Assert.That(seen, Contains.Item(tier));
        }
    }
}
