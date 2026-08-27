using System;
using System.Collections.Generic;
using Sudoku.Core.Content;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using UnityEngine;

namespace Sudoku.Game.Content
{
    /// <summary>
    /// Serves puzzles out of the baked banks and remembers which ones the
    /// player has already seen, so content feels endless.
    ///
    /// Progress is kept in PlayerPrefs for the greybox; the real per-difficulty
    /// save slots replace this.
    /// </summary>
    public sealed class PuzzleLibrary
    {
        const string ResourceRoot = "Banks/";

        readonly Dictionary<string, PuzzleBank> _banks = new Dictionary<string, PuzzleBank>();

        public PuzzleBank Bank(DifficultyTier tier, bool daily = false)
        {
            var name = (daily ? "daily-" : "main-") + tier;
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
        public Puzzle Next(DifficultyTier tier)
        {
            var bank = Bank(tier);
            var key = "played:" + tier;
            var played = PlayerPrefs.GetInt(key, 0);

            // Once the bank is exhausted, start over rather than refusing to
            // deal - 2,000 puzzles per tier is a long way past any real player.
            if (played >= bank.Count)
            {
                played = 0;
                PlayerPrefs.SetInt(key, 0);
            }

            // Walk the bank in a shuffled-but-stable order so a reinstall does
            // not replay the first puzzles in the same sequence.
            var stride = StrideFor(bank.Count);
            var offset = PlayerPrefs.GetInt("bankOffset:" + tier, -1);
            if (offset < 0)
            {
                offset = UnityEngine.Random.Range(0, bank.Count);
                PlayerPrefs.SetInt("bankOffset:" + tier, offset);
            }

            var index = (offset + played * stride) % bank.Count;
            PlayerPrefs.SetInt(key, played + 1);
            PlayerPrefs.Save();

            return bank.PuzzleAt(index);
        }

        /// <summary>Today's puzzle: same for every player, computed offline.</summary>
        public Puzzle Daily(DateTime date, out DifficultyTier tier)
        {
            tier = DailySchedule.TierFor(date.DayOfWeek);
            var bank = Bank(tier, daily: true);
            return bank.PuzzleAt(DailySchedule.IndexFor(date, bank.Count));
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
