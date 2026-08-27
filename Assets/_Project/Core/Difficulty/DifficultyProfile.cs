using System;
using Sudoku.Core.Solving;

namespace Sudoku.Core.Difficulty
{
    /// <summary>
    /// Where the tier boundaries sit. These are judgement calls, not facts, and
    /// the first setting of them will be wrong - so they live in data and the
    /// bake is re-runnable, which makes a recalibration a content change rather
    /// than a code release.
    /// </summary>
    public sealed class DifficultyProfile
    {
        public DifficultyProfile(TierRule[] tiers)
        {
            Tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
        }

        /// <summary>Tier rules, ordered easiest first.</summary>
        public TierRule[] Tiers { get; }

        public TierRule RuleFor(DifficultyTier tier)
        {
            foreach (var rule in Tiers)
                if (rule.Tier == tier)
                    return rule;
            throw new ArgumentOutOfRangeException(nameof(tier), $"No rule for {tier}.");
        }

        /// <summary>
        /// The starting calibration: one technique step per tier, symmetry given
        /// up at Expert and above. Expect telemetry to move these.
        /// </summary>
        public static DifficultyProfile Default { get; } = new DifficultyProfile(new[]
        {
            new TierRule(DifficultyTier.Easy,   Technique.NakedSingle,      symmetric: true),
            new TierRule(DifficultyTier.Medium, Technique.HiddenSingle,     symmetric: true),
            new TierRule(DifficultyTier.Hard,   Technique.LockedCandidates, symmetric: true),
            new TierRule(DifficultyTier.Expert, Technique.NakedTriple,      symmetric: false),
            new TierRule(DifficultyTier.Master, Technique.XWing,            symmetric: false),
        });
    }
}
