using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;

namespace Sudoku.Core.Content
{
    /// <summary>
    /// What a bake produced, and what it cost. Reported rather than logged
    /// away, so a bake that quietly under-delivers cannot pass for a good one.
    /// </summary>
    public sealed class BakeResult
    {
        public BakeResult(DifficultyTier tier, Puzzle[] puzzles, int requested, int attemptsUsed, int duplicatesRejected)
        {
            Tier = tier;
            Puzzles = puzzles;
            Requested = requested;
            AttemptsUsed = attemptsUsed;
            DuplicatesRejected = duplicatesRejected;
        }

        public DifficultyTier Tier { get; }
        public Puzzle[] Puzzles { get; }

        public int Requested { get; }
        public int Produced => Puzzles.Length;
        public int AttemptsUsed { get; }
        public int DuplicatesRejected { get; }

        /// <summary>True when the bake ran out of budget before filling the bank.</summary>
        public bool FellShort => Produced < Requested;

        public string Summary =>
            $"{Tier}: {Produced}/{Requested} puzzles, {AttemptsUsed} attempts, " +
            $"{DuplicatesRejected} duplicates rejected{(FellShort ? "  *** FELL SHORT ***" : "")}";
    }
}
