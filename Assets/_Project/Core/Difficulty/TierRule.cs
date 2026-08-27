using Sudoku.Core.Solving;

namespace Sudoku.Core.Difficulty
{
    /// <summary>What qualifies a puzzle for one tier.</summary>
    public sealed class TierRule
    {
        public TierRule(DifficultyTier tier, Technique hardestAllowed, bool symmetric)
        {
            Tier = tier;
            HardestAllowed = hardestAllowed;
            Symmetric = symmetric;
        }

        public DifficultyTier Tier { get; }

        /// <summary>
        /// The hardest technique a puzzle may require and still belong here.
        /// A puzzle lands in the easiest tier that tolerates what it demands.
        /// </summary>
        public Technique HardestAllowed { get; }

        /// <summary>
        /// Whether puzzles baked for this tier use 180-degree rotational clue
        /// symmetry. It looks tidier, but it caps how hard a puzzle can get,
        /// so the top tiers give it up.
        /// </summary>
        public bool Symmetric { get; }
    }
}
