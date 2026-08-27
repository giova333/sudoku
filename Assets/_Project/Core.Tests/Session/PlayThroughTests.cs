using NUnit.Framework;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Generation;
using Sudoku.Core.Model;
using Sudoku.Core.Session;

namespace Sudoku.Core.Tests.Session
{
    /// <summary>
    /// Whole-puzzle play-throughs driven exactly the way the presenter drives a
    /// session. These are the closest thing to "the game is playable" that can
    /// be asserted without pixels.
    /// </summary>
    [TestFixture]
    public class PlayThroughTests
    {
        static GameSession SessionFor(DifficultyTier tier, int seed, out Puzzle puzzle)
        {
            puzzle = PuzzleGenerator.GenerateForTier(
                seed, tier, DifficultyProfile.Default, ConstraintSet.Classic, 200);
            return new GameSession(puzzle, RulesConfig.Default);
        }

        [Test]
        public void A_puzzle_of_every_tier_can_be_solved_from_start_to_finish()
        {
            foreach (DifficultyTier tier in System.Enum.GetValues(typeof(DifficultyTier)))
            {
                var session = SessionFor(tier, 4100 + (int)tier, out var puzzle);
                session.Start();

                for (var i = 0; i < Board.CellCount; i++)
                {
                    session.Tick(0.5f);
                    if (session.ValueAt(i) == Board.Empty)
                        session.Place(i, puzzle.SolutionAt(i));
                }

                Assert.That(session.Status, Is.EqualTo(SessionStatus.Completed), $"{tier}");
                Assert.That(session.EmptyCellCount, Is.Zero);
                Assert.That(session.MistakeCount, Is.Zero);
                Assert.That(session.ElapsedSeconds, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void A_puzzle_can_be_solved_entirely_by_taking_hints()
        {
            var rules = RulesConfig.Default;
            rules.Hints = int.MaxValue;
            var puzzle = PuzzleGenerator.GenerateForTier(
                99, DifficultyTier.Medium, DifficultyProfile.Default, ConstraintSet.Classic, 200);
            var session = new GameSession(puzzle, rules);
            session.Start();

            for (var guard = 0; guard < Board.CellCount + 5 && session.EmptyCellCount > 0; guard++)
                Assert.That(session.UseHint(), Is.True, "a hint should always be available while cells remain");

            Assert.That(session.Status, Is.EqualTo(SessionStatus.Completed));
            Assert.That(session.MistakeCount, Is.Zero, "hints must never introduce a mistake");
        }

        [Test]
        public void A_realistic_messy_play_through_still_lands_on_a_solved_board()
        {
            // Notes, wrong guesses, erases and undos - the way a puzzle is
            // actually played rather than filled in index order.
            var rules = RulesConfig.Default;
            rules.MistakeLimitEnabled = false;
            var puzzle = PuzzleGenerator.GenerateForTier(
                7, DifficultyTier.Hard, DifficultyProfile.Default, ConstraintSet.Classic, 200);
            var session = new GameSession(puzzle, rules);
            session.Start();

            for (var i = 0; i < Board.CellCount; i++)
            {
                if (session.ValueAt(i) != Board.Empty) continue;

                var correct = puzzle.SolutionAt(i);
                var wrong = correct == 9 ? 1 : correct + 1;

                session.ToggleNote(i, correct);
                session.ToggleNote(i, wrong);
                session.Place(i, wrong);
                session.Undo();
                session.Place(i, correct);
                session.Tick(0.25f);
            }

            Assert.That(session.Status, Is.EqualTo(SessionStatus.Completed));
            Assert.That(session.MistakeCount, Is.EqualTo(Board.CellCount - CluesIn(puzzle)),
                "every wrong guess was counted, even the ones undone");
        }

        [Test]
        public void Running_out_of_hearts_stops_the_game_mid_puzzle()
        {
            var rules = RulesConfig.Default;
            rules.Hearts = 3;
            var session = SessionFor(DifficultyTier.Easy, 31, out var puzzle);
            session = new GameSession(puzzle, rules);
            session.Start();

            var burned = 0;
            for (var i = 0; i < Board.CellCount && session.Status == SessionStatus.InProgress; i++)
            {
                if (puzzle.IsGiven(i)) continue;
                var wrong = puzzle.SolutionAt(i) == 9 ? 1 : puzzle.SolutionAt(i) + 1;
                if (session.Place(i, wrong)) burned++;
            }

            Assert.That(burned, Is.EqualTo(3));
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Failed));
            Assert.That(session.Place(80, 1), Is.False, "the board is closed once the game is lost");
        }

        static int CluesIn(Puzzle puzzle)
        {
            var clues = 0;
            for (var i = 0; i < Board.CellCount; i++)
                if (puzzle.IsGiven(i)) clues++;
            return clues;
        }
    }
}
