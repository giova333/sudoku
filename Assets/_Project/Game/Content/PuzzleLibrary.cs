using System;
using System.Collections.Generic;
using Sudoku.Core.Content;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using Sudoku.Game.Save;
using UnityEngine;

namespace Sudoku.Game.Content
{
    /// <summary>
    /// Serves puzzles out of the baked banks and remembers which ones the
    /// player has already seen, so content feels endless.
    ///
    /// That memory lives in the save file rather than in engine preferences:
    /// "never serve the same puzzle twice" should survive exactly as long as an
    /// in-progress puzzle does, and travel with it.
    /// </summary>
    public sealed class PuzzleLibrary
    {
        const string ResourceRoot = "Banks/";

        readonly SaveStore _saves;
        readonly Dictionary<string, PuzzleBank> _banks = new Dictionary<string, PuzzleBank>();

        public PuzzleLibrary(SaveStore saves)
        {
            _saves = saves ?? throw new ArgumentNullException(nameof(saves));
        }

        /// <summary>The resource name of a bank, recorded in a save slot as its puzzle's provenance.</summary>
        public static string BankName(DifficultyTier tier, bool daily = false) =>
            (daily ? "daily-" : "main-") + tier;

        public PuzzleBank Bank(DifficultyTier tier, bool daily = false)
        {
            var name = BankName(tier, daily);
            if (_banks.TryGetValue(name, out var cached))
                return cached;

            var asset = Resources.Load<TextAsset>(ResourceRoot + name);
            if (asset == null)
                throw new InvalidOperationException(
                    $"Puzzle bank '{name}' is missing. Run Sudoku > Bake Puzzle Banks... or tools/bake.sh.");

            var bank = PuzzleBankSerializer.Read(asset.bytes);
            _banks[name] = bank;
            return bank;
        }

        /// <summary>A puzzle of this tier the player has not been served before.</summary>
        public Puzzle Next(DifficultyTier tier) => Next(tier, out _);

        /// <summary>
        /// As <see cref="Next(DifficultyTier)"/>, and also reports where in the
        /// bank the puzzle came from so a save slot can record its provenance.
        /// </summary>
        public Puzzle Next(DifficultyTier tier, out int bankIndex)
        {
            var bank = Bank(tier);
            var progress = _saves.Data.ProgressFor(tier);

            // Once the bank is exhausted, start over rather than refusing to
            // deal - 2,000 puzzles per tier is a long way past any real player.
            if (progress.Played >= bank.Count)
                progress.Played = 0;

            // Walk the bank in a shuffled-but-stable order so a reinstall does
            // not replay the first puzzles in the same sequence.
            if (progress.Offset < 0)
                progress.Offset = UnityEngine.Random.Range(0, bank.Count);

            var stride = StrideFor(bank.Count);
            bankIndex = (progress.Offset + progress.Played * stride) % bank.Count;
            progress.Played++;
            _saves.Touch();

            return bank.PuzzleAt(bankIndex);
        }

        /// <summary>Today's puzzle: same for every player, computed offline.</summary>
        public Puzzle Daily(DateTime date, out DifficultyTier tier) => Daily(date, out tier, out _);

        /// <summary>
        /// As <see cref="Daily(DateTime, out DifficultyTier)"/>, and also
        /// reports the bank index the daily slot records.
        /// </summary>
        public Puzzle Daily(DateTime date, out DifficultyTier tier, out int bankIndex)
        {
            tier = DailySchedule.TierFor(date.DayOfWeek);
            var bank = Bank(tier, daily: true);
            bankIndex = DailySchedule.IndexFor(date, bank.Count);
            return bank.PuzzleAt(bankIndex);
        }

        /// <summary>
        /// A step coprime with the bank size, so repeatedly adding it visits
        /// every puzzle exactly once before repeating.
        /// </summary>
        static int StrideFor(int count)
        {
            for (var stride = count / 2 + 1; stride < count; stride++)
                if (Gcd(stride, count) == 1)
                    return stride;
            return 1;
        }

        static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                var t = b;
                b = a % b;
                a = t;
            }
            return a;
        }
    }
}
