using System;
using System.Collections.Generic;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Generation;
using Sudoku.Core.Session;

namespace Sudoku.Core.Copy
{
    /// <summary>
    /// Chooses what the results card says about a solve.
    ///
    /// One instance lives for the whole run of the app, because the thing worth
    /// getting right is that a line is never read twice in a session - story
    /// 51. Shuffling a pool and hoping does not achieve that; remembering what
    /// has already been said does, so the picker holds that memory and is the
    /// reason it is an object rather than a static method.
    ///
    /// It has no engine dependency on purpose: bucketing and no-repeat are
    /// rules, and rules belong where they can be tested.
    /// </summary>
    public sealed class ReactionPicker
    {
        /// <summary>What counts as "a lot" of either. Three is the point at
        /// which a run stops being clean and starts being a story.</summary>
        const int ManyMistakes = 3;
        const int ManyHints = 3;

        /// <summary>
        /// Par times per tier, in seconds, indexed by <see cref="DifficultyTier"/>.
        /// These are judgement calls in the same spirit as
        /// <see cref="DifficultyProfile.Default"/> - they decide only which pool
        /// a line comes from, so being a little wrong costs a slightly odd joke
        /// rather than a wrong puzzle.
        /// </summary>
        static readonly float[] FastSeconds = { 180f, 300f, 420f, 600f, 900f };
        static readonly float[] SlowSeconds = { 600f, 900f, 1200f, 1800f, 2400f };

        readonly DeterministicRandom _random;

        /// <summary>Every line said so far this session. One set rather than one
        /// per bucket, because the pools share no lines and a repeat reads as a
        /// repeat wherever it came from.</summary>
        readonly HashSet<string> _spoken = new HashSet<string>();

        /// <summary>Seeded from the clock, so two players do not read the same
        /// line first.</summary>
        public ReactionPicker() : this(Environment.TickCount)
        {
        }

        public ReactionPicker(int seed)
        {
            _random = new DeterministicRandom(seed);
        }

        /// <summary>
        /// Which pool a solve draws from.
        ///
        /// The order is deliberate: what the player did outranks what the clock
        /// says, because "you made six mistakes" is a more interesting
        /// observation than "that was quick", and a run cannot be both perfect
        /// and mistake-heavy. A personal best counts as fast whatever the clock
        /// reads - fast is relative to the player, not to the tier.
        /// </summary>
        public static ReactionBucket BucketFor(PuzzleResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (result.MistakeCount >= ManyMistakes) return ReactionBucket.MistakeHeavy;
            if (result.HintsUsed >= ManyHints) return ReactionBucket.HintHeavy;
            if (result.IsPerfect) return ReactionBucket.Perfect;

            var tier = Index(result.Tier);
            if (result.IsNewBest || result.ElapsedSeconds <= FastSeconds[tier]) return ReactionBucket.Fast;
            if (result.ElapsedSeconds >= SlowSeconds[tier]) return ReactionBucket.Slow;

            return ReactionBucket.Steady;
        }

        /// <summary>
        /// The line for this solve, guaranteed not to be one already read this
        /// session until its pool has run dry.
        ///
        /// A pool holding seven lines cannot survive an eighth solve of the same
        /// shape, so the promise is the honest one: every line in a bucket is
        /// spent before any of them comes back. Running a bucket dry retires
        /// only that bucket's history, so the lines the player has read
        /// elsewhere stay retired.
        /// </summary>
        public string Next(PuzzleResult result)
        {
            var pool = CopyTable.Reactions(BucketFor(result));

            var unsaid = new List<string>(pool.Count);
            foreach (var line in pool)
                if (!_spoken.Contains(line))
                    unsaid.Add(line);

            if (unsaid.Count == 0)
            {
                foreach (var line in pool)
                {
                    _spoken.Remove(line);
                    unsaid.Add(line);
                }
            }

            var chosen = unsaid[_random.Next(unsaid.Count)];
            _spoken.Add(chosen);
            return chosen;
        }

        static int Index(DifficultyTier tier)
        {
            var index = (int)tier;
            if (index < 0) return 0;
            if (index >= FastSeconds.Length) return FastSeconds.Length - 1;
            return index;
        }
    }
}
