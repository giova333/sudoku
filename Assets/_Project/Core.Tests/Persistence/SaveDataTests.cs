using NUnit.Framework;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using Sudoku.Core.Persistence;
using Sudoku.Core.Session;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Persistence
{
    /// <summary>
    /// The questions a Continue button and a difficulty picker ask of the save
    /// file. They are asked before anything has been restored, so they are
    /// answered off the stored slots rather than by rebuilding a session.
    /// </summary>
    [TestFixture]
    public class SaveDataTests
    {
        static Puzzle ClassicPuzzle() =>
            Puzzle.FromStrings(KnownPuzzles.ClassicClues, KnownPuzzles.ClassicSolution);

        static SaveSlot SlotFor(DifficultyTier tier) =>
            SaveSlot.ForTier(tier, "main-" + tier, 1, ClassicPuzzle(), RulesConfig.Default);

        [Test]
        public void A_tier_the_player_has_never_opened_has_nothing_waiting()
        {
            Assert.That(new SaveData().ResumableFor(DifficultyTier.Hard), Is.Null);
        }

        [Test]
        public void A_half_finished_puzzle_is_waiting_under_its_own_tier()
        {
            var data = new SaveData();
            data.PutSlot(SlotFor(DifficultyTier.Hard));

            Assert.That(data.ResumableFor(DifficultyTier.Hard), Is.Not.Null);
            Assert.That(data.ResumableFor(DifficultyTier.Easy), Is.Null,
                "one tier's puzzle should never show up under another");
        }

        [Test]
        public void A_finished_puzzle_is_not_waiting_under_its_tier()
        {
            var data = new SaveData();
            var slot = SlotFor(DifficultyTier.Master);
            slot.Session.Status = SessionStatus.Completed;
            data.PutSlot(slot);

            Assert.That(data.ResumableFor(DifficultyTier.Master), Is.Null);
            Assert.That(data.SlotFor(DifficultyTier.Master), Is.Not.Null,
                "the slot is still there - it is only no longer resumable");
        }

        [Test]
        public void A_slot_quotes_the_time_the_player_has_spent_on_its_puzzle()
        {
            var slot = SlotFor(DifficultyTier.Expert);
            slot.Session.ElapsedSeconds = 754f;

            Assert.That(slot.ElapsedSeconds, Is.EqualTo(754f));
        }

        [Test]
        public void A_slot_with_nothing_recorded_yet_quotes_no_elapsed_time()
        {
            var slot = SlotFor(DifficultyTier.Easy);
            slot.Session = null;

            Assert.That(slot.ElapsedSeconds, Is.Zero);
        }

        [Test]
        public void A_run_that_ended_out_of_hearts_is_not_offered_to_be_continued()
        {
            var data = new SaveData();
            var slot = SlotFor(DifficultyTier.Hard);
            slot.Session.Status = SessionStatus.Failed;
            data.PutSlot(slot);

            Assert.That(data.ResumableFor(DifficultyTier.Hard), Is.Null,
                "there is nothing to carry on with once the hearts are gone");
            Assert.That(data.MostRecent(), Is.Null);
        }

        [Test]
        public void A_run_that_ended_out_of_hearts_keeps_its_puzzle_to_be_started_over()
        {
            var data = new SaveData();
            var slot = SlotFor(DifficultyTier.Hard);
            slot.Session.Status = SessionStatus.Failed;
            data.PutSlot(slot);

            var kept = data.SlotFor(DifficultyTier.Hard);
            Assert.That(kept, Is.Not.Null, "losing must not throw the puzzle away");
            Assert.That(kept.CanRestart, Is.True);
        }

        [Test]
        public void A_puzzle_still_being_played_is_not_offered_to_be_started_over()
        {
            Assert.That(SlotFor(DifficultyTier.Easy).CanRestart, Is.False);
        }

        [Test]
        public void A_finished_puzzle_is_not_offered_to_be_started_over()
        {
            var slot = SlotFor(DifficultyTier.Easy);
            slot.Session.Status = SessionStatus.Completed;

            Assert.That(slot.CanRestart, Is.False,
                "a solved board is finished with, not waiting to be replayed");
        }

        [Test]
        public void A_slot_names_its_puzzle_by_the_bank_it_was_dealt_from()
        {
            var slot = SaveSlot.ForTier(DifficultyTier.Expert, "main-Expert", 42,
                ClassicPuzzle(), RulesConfig.Default);

            Assert.That(slot.PuzzleId, Is.EqualTo("main-Expert#42"));
        }
    }
}
