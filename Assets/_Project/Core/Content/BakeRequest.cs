using Sudoku.Core.Difficulty;

namespace Sudoku.Core.Content
{
    /// <summary>One tier's worth of work for the bake.</summary>
    public sealed class BakeRequest
    {
        public BakeRequest(DifficultyTier tier, int count, int seed)
        {
            Tier = tier;
            Count = count;
            Seed = seed;
        }

        public DifficultyTier Tier { get; }
        public int Count { get; }

        /// <summary>Fixed so the bank can be reproduced exactly.</summary>
        public int Seed { get; }

        /// <summary>
        /// A ceiling on generation attempts, so a mis-set difficulty profile
        /// makes the bake report failure instead of running forever.
        /// </summary>
        public int MaxTotalAttempts { get; set; } = 100000;

        public int MaxAttemptsPerPuzzle { get; set; } = 200;
    }
}
