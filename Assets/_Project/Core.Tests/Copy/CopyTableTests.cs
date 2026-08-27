using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sudoku.Core.Copy;
using Sudoku.Core.Difficulty;

namespace Sudoku.Core.Tests.Copy
{
    /// <summary>
    /// What can be checked about a voice mechanically: that the pools are the
    /// size the spec asks for, that no line is duplicated, and that nothing
    /// shouts. Whether the lines are actually funny is not testable and is not
    /// attempted here.
    /// </summary>
    [TestFixture]
    public class CopyTableTests
    {
        [Test]
        public void Every_outcome_bucket_offers_between_five_and_ten_reactions()
        {
            foreach (var bucket in CopyTable.Buckets)
            {
                var pool = CopyTable.Reactions(bucket);
                Assert.That(pool.Count, Is.GreaterThanOrEqualTo(5), $"{bucket} has too few lines");
                Assert.That(pool.Count, Is.LessThanOrEqualTo(10), $"{bucket} has too many lines");
            }
        }

        [Test]
        public void No_reaction_line_appears_in_two_pools()
        {
            // The picker keeps one "already said" set for every bucket, which is
            // only correct while the pools share no lines.
            var seen = new HashSet<string>();

            foreach (var bucket in CopyTable.Buckets)
            foreach (var line in CopyTable.Reactions(bucket))
                Assert.That(seen.Add(line), Is.True, $"duplicated line: {line}");
        }

        [Test]
        public void No_reaction_line_is_blank()
        {
            foreach (var bucket in CopyTable.Buckets)
            foreach (var line in CopyTable.Reactions(bucket))
                Assert.That(string.IsNullOrWhiteSpace(line), Is.False, $"{bucket} has a blank line");
        }

        [Test]
        public void The_voice_never_exclaims()
        {
            // Deadpan is the whole brief, and an exclamation mark is the fastest
            // way to lose it. Cheap to check, easy to reintroduce by accident.
            foreach (var bucket in CopyTable.Buckets)
            foreach (var line in CopyTable.Reactions(bucket))
                Assert.That(line.Contains("!"), Is.False, $"exclamatory line: {line}");
        }

        [Test]
        public void The_voice_stays_within_plain_ascii()
        {
            // No emoji and no smart punctuation: the greybox font has neither,
            // and a line that renders as a box is worse than no line.
            foreach (var bucket in CopyTable.Buckets)
            foreach (var line in CopyTable.Reactions(bucket))
            foreach (var character in line)
                Assert.That(character, Is.LessThan((char)128), $"non-ascii character in: {line}");
        }

        [Test]
        public void Nothing_inside_the_puzzle_says_a_sentence()
        {
            // Story 72. In-puzzle text is labels and counters; the moment one of
            // them ends in a full stop it has become a remark, and a remark
            // while the player is solving is an interruption.
            foreach (var line in CopyTable.InPuzzle)
            {
                Assert.That(line.Contains("."), Is.False, $"in-puzzle copy reads as prose: {line}");
                Assert.That(line.Contains("!"), Is.False, $"in-puzzle copy exclaims: {line}");
            }
        }

        [Test]
        public void No_reaction_line_is_ever_shown_inside_the_puzzle()
        {
            var inPuzzle = new HashSet<string>(CopyTable.InPuzzle);

            foreach (var bucket in CopyTable.Buckets)
            foreach (var line in CopyTable.Reactions(bucket))
                Assert.That(inPuzzle.Contains(line), Is.False, $"a reaction leaked into the board: {line}");
        }

        [Test]
        public void The_status_strip_carries_no_mistake_count_for_a_self_checked_game()
        {
            // Story 21. A count that climbs the instant a wrong digit lands is
            // immediate mistake feedback written in numbers, so a player who has
            // turned that feedback off must not be handed it in the HUD.
            var strip = CopyTable.HudStatusUnchecked("04:12", 3, 27);

            Assert.That(strip.Contains("Mistakes"), Is.False, $"the count survived: {strip}");
            Assert.That(strip.Contains("Hearts 3"), Is.True,
                "hearts are a resource the player is spending, not a telling-off");
        }

        [Test]
        public void Every_difficulty_tier_has_a_name()
        {
            foreach (DifficultyTier tier in Enum.GetValues(typeof(DifficultyTier)))
                Assert.That(string.IsNullOrEmpty(CopyTable.Tier(tier)), Is.False, $"{tier} is unnamed");
        }

        [Test]
        public void Counters_read_as_english_rather_than_as_a_count()
        {
            Assert.That(CopyTable.ResultsCounters(1, 1), Is.EqualTo("1 mistake    1 hint"));
            Assert.That(CopyTable.ResultsCounters(0, 2), Is.EqualTo("0 mistakes    2 hints"));
            Assert.That(CopyTable.GameOverMistakes(1), Is.EqualTo("1 mistake"));
            Assert.That(CopyTable.GameOverMistakes(3), Is.EqualTo("3 mistakes"));
        }
    }
}
