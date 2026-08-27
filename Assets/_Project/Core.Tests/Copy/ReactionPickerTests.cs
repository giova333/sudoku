using System.Collections.Generic;
using NUnit.Framework;
using Sudoku.Core.Copy;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Session;

namespace Sudoku.Core.Tests.Copy
{
    /// <summary>
    /// Stories 50 and 51: the card says something about the solve, and it does
    /// not say the same thing twice in one sitting.
    /// </summary>
    [TestFixture]
    public class ReactionPickerTests
    {
        static PuzzleResult Result(DifficultyTier tier = DifficultyTier.Easy, float elapsed = 400f,
            int mistakes = 1, int hints = 0, bool isNewBest = false) =>
            new PuzzleResult(tier, elapsed, mistakes, hints, 300f, isNewBest);

        /// <summary>Easy's par is 180 seconds fast, 600 slow.</summary>
        static PuzzleResult Fast() => Result(elapsed: 90f);

        static PuzzleResult Slow() => Result(elapsed: 900f);

        [Test]
        public void A_quick_solve_is_called_quick()
        {
            Assert.That(ReactionPicker.BucketFor(Fast()), Is.EqualTo(ReactionBucket.Fast));
        }

        [Test]
        public void A_long_solve_is_called_long()
        {
            Assert.That(ReactionPicker.BucketFor(Slow()), Is.EqualTo(ReactionBucket.Slow));
        }

        [Test]
        public void No_mistakes_and_no_hints_is_a_perfect_run()
        {
            var result = Result(elapsed: 900f, mistakes: 0, hints: 0);
            Assert.That(ReactionPicker.BucketFor(result), Is.EqualTo(ReactionBucket.Perfect),
                "a clean run is worth remarking on however long it took");
        }

        [Test]
        public void What_the_player_did_outranks_what_the_clock_says()
        {
            var messyButFast = Result(elapsed: 60f, mistakes: 5);
            Assert.That(ReactionPicker.BucketFor(messyButFast), Is.EqualTo(ReactionBucket.MistakeHeavy));

            var helpedButFast = Result(elapsed: 60f, mistakes: 0, hints: 4);
            Assert.That(ReactionPicker.BucketFor(helpedButFast), Is.EqualTo(ReactionBucket.HintHeavy));
        }

        [Test]
        public void A_personal_best_counts_as_quick_however_long_it_took()
        {
            // Fast is relative to the player. The first Master solve is a record
            // at any time on the clock.
            var record = Result(DifficultyTier.Master, elapsed: 3000f, isNewBest: true);
            Assert.That(ReactionPicker.BucketFor(record), Is.EqualTo(ReactionBucket.Fast));
        }

        [Test]
        public void A_harder_tier_is_given_longer_before_it_is_called_slow()
        {
            var elapsed = 700f;
            Assert.That(ReactionPicker.BucketFor(Result(DifficultyTier.Easy, elapsed)),
                Is.EqualTo(ReactionBucket.Slow));
            Assert.That(ReactionPicker.BucketFor(Result(DifficultyTier.Master, elapsed)),
                Is.EqualTo(ReactionBucket.Fast));
        }

        [Test]
        public void An_unremarkable_solve_still_gets_a_line()
        {
            var ordinary = Result(elapsed: 400f, mistakes: 1);
            Assert.That(ReactionPicker.BucketFor(ordinary), Is.EqualTo(ReactionBucket.Steady));
            Assert.That(new ReactionPicker(1).Next(ordinary), Is.Not.Empty,
                "a blank card is worse than a flat line");
        }

        [Test]
        public void The_line_comes_from_the_pool_the_solve_belongs_to()
        {
            var picker = new ReactionPicker(11);
            var result = Slow();
            Assert.That(CopyTable.Reactions(ReactionBucket.Slow), Contains.Item(picker.Next(result)));
        }

        [Test]
        public void A_line_never_repeats_until_its_pool_is_spent()
        {
            var picker = new ReactionPicker(4242);
            var pool = CopyTable.Reactions(ReactionBucket.Fast);
            var said = new HashSet<string>();

            for (var i = 0; i < pool.Count; i++)
                Assert.That(said.Add(picker.Next(Fast())), Is.True,
                    "a line came back before the pool ran out");
        }

        [Test]
        public void A_spent_pool_keeps_talking_rather_than_going_quiet()
        {
            var picker = new ReactionPicker(9);
            var pool = CopyTable.Reactions(ReactionBucket.Fast);

            for (var i = 0; i < pool.Count; i++)
                picker.Next(Fast());

            // Seven lines cannot cover an eighth Easy sprint, so the pool comes
            // back rather than the card falling silent.
            Assert.That(pool, Contains.Item(picker.Next(Fast())));
        }

        [Test]
        public void Spending_one_pool_does_not_un_retire_the_others()
        {
            var picker = new ReactionPicker(77);
            var firstSlowLine = picker.Next(Slow());

            var fast = CopyTable.Reactions(ReactionBucket.Fast).Count;
            for (var i = 0; i <= fast; i++)
                picker.Next(Fast());

            var slow = CopyTable.Reactions(ReactionBucket.Slow).Count;
            for (var i = 1; i < slow; i++)
                Assert.That(picker.Next(Slow()), Is.Not.EqualTo(firstSlowLine),
                    "running the fast pool dry should not forget what was said about a slow solve");
        }

        [Test]
        public void The_same_seed_says_the_same_things()
        {
            var one = new ReactionPicker(31337);
            var two = new ReactionPicker(31337);

            for (var i = 0; i < 5; i++)
                Assert.That(one.Next(Fast()), Is.EqualTo(two.Next(Fast())));
        }
    }
}
