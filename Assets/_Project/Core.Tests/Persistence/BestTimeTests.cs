using NUnit.Framework;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Persistence;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Persistence
{
    /// <summary>
    /// A record is only a reason to replay a tier if it outlives the launch that
    /// set it, so best times live in the save file and are tested the same way
    /// the slots are: written, read back, and asserted intact.
    /// </summary>
    [TestFixture]
    public class BestTimeTests
    {
        static SaveData RoundTrip(SaveData data) => SaveSerializer.Read(SaveSerializer.Write(data));

        [Test]
        public void A_tier_that_has_never_been_finished_has_no_best_time()
        {
            var data = new SaveData();

            Assert.That(data.BestTimeFor(DifficultyTier.Hard).IsSet, Is.False);
        }

        [Test]
        public void The_first_solve_of_a_tier_sets_its_record()
        {
            var data = new SaveData();

            Assert.That(data.RecordBestTime(DifficultyTier.Hard, 400f), Is.True);
            Assert.That(data.BestTimeFor(DifficultyTier.Hard).Seconds, Is.EqualTo(400f));
        }

        [Test]
        public void A_faster_solve_beats_the_record_and_replaces_it()
        {
            var data = new SaveData();
            data.RecordBestTime(DifficultyTier.Hard, 400f);

            Assert.That(data.RecordBestTime(DifficultyTier.Hard, 350f), Is.True, "beaten");
            Assert.That(data.BestTimeFor(DifficultyTier.Hard).Seconds, Is.EqualTo(350f));
        }

        [Test]
        public void A_slower_solve_leaves_the_record_where_it_was()
        {
            var data = new SaveData();
            data.RecordBestTime(DifficultyTier.Hard, 350f);

            Assert.That(data.RecordBestTime(DifficultyTier.Hard, 400f), Is.False, "not beaten");
            Assert.That(data.BestTimeFor(DifficultyTier.Hard).Seconds, Is.EqualTo(350f));
        }

        [Test]
        public void Matching_the_record_exactly_is_not_beating_it()
        {
            var data = new SaveData();
            data.RecordBestTime(DifficultyTier.Hard, 350f);

            Assert.That(data.RecordBestTime(DifficultyTier.Hard, 350f), Is.False);
        }

        [Test]
        public void Each_difficulty_keeps_its_own_record()
        {
            var data = new SaveData();
            data.RecordBestTime(DifficultyTier.Easy, 120f);
            data.RecordBestTime(DifficultyTier.Master, 1800f);

            Assert.That(data.BestTimeFor(DifficultyTier.Easy).Seconds, Is.EqualTo(120f), "Easy");
            Assert.That(data.BestTimeFor(DifficultyTier.Master).Seconds, Is.EqualTo(1800f), "Master");
        }

        [Test]
        public void A_record_survives_being_written_and_read_back()
        {
            var data = new SaveData();
            data.RecordBestTime(DifficultyTier.Expert, 903.5f);

            Assert.That(RoundTrip(data).BestTimeFor(DifficultyTier.Expert).Seconds,
                Is.EqualTo(903.5f));
        }

        [Test]
        public void A_tier_never_finished_writes_no_record_at_all()
        {
            var data = new SaveData();
            // Asking creates the entry; an unset one is not worth a line in the file.
            data.BestTimeFor(DifficultyTier.Medium);

            Assert.That(RoundTrip(data).BestTimes, Is.Empty);
        }

        [Test]
        public void A_save_from_before_records_existed_loads_with_none()
        {
            var data = SaveSerializer.Read(SavePayloads.SchemaVersionTwo);

            Assert.That(data.BestTimes, Is.Empty, "nothing to carry across");
            Assert.That(data.SchemaVersion, Is.EqualTo(SaveSerializer.CurrentSchemaVersion));
        }

        [Test]
        public void A_save_from_before_records_existed_keeps_what_it_did_hold()
        {
            var data = SaveSerializer.Read(SavePayloads.SchemaVersionTwo);

            Assert.That(data.ProgressFor(DifficultyTier.Hard).Played, Is.EqualTo(4));
        }
    }
}
