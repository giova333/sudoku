using Sudoku.Core.Difficulty;

namespace Sudoku.Core.Persistence
{
    /// <summary>
    /// The fastest solve of one difficulty. It lives in the save file next to
    /// the in-progress slots rather than in engine preferences, so a record and
    /// the puzzles that produced it are backed up, moved and cleared together.
    /// </summary>
    public sealed class BestTime
    {
        public BestTime(DifficultyTier tier)
        {
            Tier = tier;
        }

        public DifficultyTier Tier { get; }

        /// <summary>
        /// The record in seconds, or zero when this tier has never been
        /// finished. Zero rather than a sentinel because no solve takes no time,
        /// so the empty case needs no extra flag to read correctly.
        /// </summary>
        public float Seconds { get; set; }

        public bool IsSet => Seconds > 0f;

        /// <summary>
        /// Counts a finished solve. Returns true when it beat what stood before,
        /// which is the only thing the results card needs in order to know
        /// whether to celebrate. A first solve is a record - there was nothing
        /// to beat, and the player still has not seen this number before.
        /// </summary>
        public bool Record(float seconds)
        {
            if (seconds <= 0f)
                return false;
            if (IsSet && seconds >= Seconds)
                return false;

            Seconds = seconds;
            return true;
        }
    }
}
