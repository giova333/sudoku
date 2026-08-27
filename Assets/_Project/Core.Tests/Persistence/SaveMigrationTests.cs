using NUnit.Framework;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Persistence;
using Sudoku.Core.Session;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Persistence
{
    /// <summary>
    /// An app update must never wipe a save. These read a hand-written payload
    /// from the previous schema and assert the puzzle comes back intact.
    /// </summary>
    [TestFixture]
    public class SaveMigrationTests
    {
        static SaveData VersionOne() => SaveSerializer.Read(SavePayloads.SchemaVersionOne);

        [Test]
        public void A_save_from_the_previous_schema_still_loads()
        {
            Assert.That(VersionOne().SlotFor(DifficultyTier.Easy), Is.Not.Null);
        }

        [Test]
        public void A_migrated_save_is_stamped_with_the_current_schema_version()
        {
            Assert.That(VersionOne().SchemaVersion, Is.EqualTo(SaveSerializer.CurrentSchemaVersion));
        }

        [Test]
        public void The_single_slot_of_the_previous_schema_becomes_that_difficultys_slot()
        {
            var slot = VersionOne().SlotFor(DifficultyTier.Easy);

            Assert.That(slot.BankIndex, Is.EqualTo(137));
        }

        [Test]
        public void A_migrated_puzzle_keeps_the_digits_the_player_had_entered()
        {
            var session = VersionOne().SlotFor(DifficultyTier.Easy).ToSession();

            // Cell 2 was the player's correct 4; cell 3 was their wrong 9.
            Assert.That(session.ValueAt(2), Is.EqualTo(4), "cell 2");
            Assert.That(session.ValueAt(3), Is.EqualTo(9), "cell 3");
        }

        [Test]
        public void A_migrated_puzzle_keeps_its_pencil_marks()
        {
            var session = VersionOne().SlotFor(DifficultyTier.Easy).ToSession();

            // Cell 5 was pencilled with 1 and 2, cell 8 with 3 and 5.
            Assert.That(session.NotesAt(5), Is.EqualTo(3), "cell 5");
            Assert.That(session.NotesAt(8), Is.EqualTo(20), "cell 8");
        }

        [Test]
        public void A_migrated_puzzle_keeps_its_clock_and_counters()
        {
            var session = VersionOne().SlotFor(DifficultyTier.Easy).ToSession();

            Assert.That(session.ElapsedSeconds, Is.EqualTo(91.5f), "elapsed");
            Assert.That(session.HeartsRemaining, Is.EqualTo(2), "hearts");
            Assert.That(session.MistakeCount, Is.EqualTo(1), "mistakes");
            Assert.That(session.Status, Is.EqualTo(SessionStatus.InProgress), "status");
        }

        [Test]
        public void A_migrated_puzzle_keeps_its_undo_stack()
        {
            var session = VersionOne().SlotFor(DifficultyTier.Easy).ToSession();

            Assert.That(session.UndoDepth, Is.EqualTo(2));
        }

        [Test]
        public void Undoing_a_migrated_puzzle_reverses_the_move_it_recorded()
        {
            var session = VersionOne().SlotFor(DifficultyTier.Easy).ToSession();

            session.Undo();

            Assert.That(session.ValueAt(3), Is.EqualTo(0), "the wrong 9 should be gone");
        }

        [Test]
        public void A_migrated_save_starts_with_no_played_puzzle_tracking()
        {
            // Version 1 left those counts in engine preferences, so there is
            // nothing to carry across - the walk simply starts again.
            Assert.That(VersionOne().ProgressFor(DifficultyTier.Easy).Offset, Is.EqualTo(-1));
        }

        [Test]
        public void A_migrated_save_is_written_back_in_the_current_format()
        {
            var rewritten = SaveSerializer.Read(SaveSerializer.Write(VersionOne()));

            Assert.That(rewritten.SlotFor(DifficultyTier.Easy).ToSession().UndoDepth, Is.EqualTo(2));
        }
    }
}
