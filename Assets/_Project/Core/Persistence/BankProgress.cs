using Sudoku.Core.Difficulty;

namespace Sudoku.Core.Persistence
{
    /// <summary>
    /// How far through one tier's bank the player has walked. This lives in the
    /// save file rather than in engine preferences so that "never serve the same
    /// puzzle twice" survives exactly as long as the saves do, and moves with
    /// them.
    /// </summary>
    public sealed class BankProgress
    {
        public BankProgress(DifficultyTier tier)
        {
            Tier = tier;
        }

        public DifficultyTier Tier { get; }

        /// <summary>Puzzles dealt from this tier so far.</summary>
        public int Played { get; set; }

        /// <summary>
        /// Where in the bank this install started walking, negative until one
        /// has been chosen. A per-install offset is what stops a reinstall
        /// replaying the same opening run of puzzles.
        /// </summary>
        public int Offset { get; set; } = -1;
    }
}
