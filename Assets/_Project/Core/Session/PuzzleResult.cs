using Sudoku.Core.Difficulty;

namespace Sudoku.Core.Session
{
    /// <summary>
    /// How one solve went, frozen at the moment it ended. The results card
    /// renders this and nothing else, so a finished session can be put down
    /// without the card losing what it was showing.
    ///
    /// It lives in Core because everything on it is a fact about the run rather
    /// than a fact about the screen - which is also what lets the copy layer
    /// (ticket #12) pick its reaction from this object without touching a view.
    /// </summary>
    public sealed class PuzzleResult
    {
        public PuzzleResult(DifficultyTier tier, float elapsedSeconds, int mistakeCount,
            int hintsUsed, float bestSeconds, bool isNewBest)
        {
            Tier = tier;
            ElapsedSeconds = elapsedSeconds;
            MistakeCount = mistakeCount;
            HintsUsed = hintsUsed;
            BestSeconds = bestSeconds;
            IsNewBest = isNewBest;
        }

        public DifficultyTier Tier { get; }

        public float ElapsedSeconds { get; }

        public int MistakeCount { get; }

        public int HintsUsed { get; }

        /// <summary>
        /// The best time for this tier now that this solve has been counted, so
        /// a new record and the number it set are the same number. Zero when the
        /// tier has never been finished, which cannot happen on a card that is
        /// being shown.
        /// </summary>
        public float BestSeconds { get; }

        /// <summary>True when this solve beat what came before it.</summary>
        public bool IsNewBest { get; }

        /// <summary>
        /// No mistakes and no hints. Kept here rather than recomputed by every
        /// caller because "perfect" is a rule about a run, and the results card,
        /// the copy table and any future achievement all have to agree on it.
        /// </summary>
        public bool IsPerfect => MistakeCount == 0 && HintsUsed == 0;
    }
}
