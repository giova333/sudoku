using System;
using Sudoku.Core.Difficulty;

namespace Sudoku.Core.Content
{
    /// <summary>
    /// Maps a calendar date to a puzzle, deterministically and offline. Every
    /// player worldwide gets the same daily puzzle without a server round-trip,
    /// and the mapping can be reproduced years later.
    /// </summary>
    public static class DailySchedule
    {
        /// <summary>
        /// Which puzzle in the daily bank belongs to a date. Only the calendar
        /// date is used - never the time of day, and never a timestamp - so the
        /// answer cannot drift with a device's clock or timezone.
        /// </summary>
        public static int IndexFor(DateTime date, int bankSize)
        {
            if (bankSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bankSize));

            // FNV-1a over the calendar date. Hashing rather than using the day
            // number directly keeps consecutive days far apart in the bank, so
            // a player cannot infer tomorrow's puzzle from today's.
            unchecked
            {
                var hash = 2166136261u;
                hash = Mix(hash, (uint)date.Year);
                hash = Mix(hash, (uint)date.Month);
                hash = Mix(hash, (uint)date.Day);
                return (int)(hash % (uint)bankSize);
            }
        }

        static uint Mix(uint hash, uint value)
        {
            unchecked
            {
                for (var shift = 0; shift < 32; shift += 8)
                {
                    hash ^= (value >> shift) & 0xFF;
                    hash *= 16777619u;
                }
                return hash;
            }
        }

        /// <summary>
        /// The difficulty curve across a week: gentle at the start, hardest on
        /// Sunday. Gives the daily a rhythm and a reason to come back on a day
        /// that suits the player's appetite.
        /// </summary>
        public static DifficultyTier TierFor(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday: return DifficultyTier.Easy;
                case DayOfWeek.Tuesday: return DifficultyTier.Easy;
                case DayOfWeek.Wednesday: return DifficultyTier.Medium;
                case DayOfWeek.Thursday: return DifficultyTier.Medium;
                case DayOfWeek.Friday: return DifficultyTier.Hard;
                case DayOfWeek.Saturday: return DifficultyTier.Expert;
                default: return DifficultyTier.Master;
            }
        }
    }
}
