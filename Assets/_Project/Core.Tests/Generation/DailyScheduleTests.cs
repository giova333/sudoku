using System;
using NUnit.Framework;
using Sudoku.Core.Content;
using Sudoku.Core.Difficulty;

namespace Sudoku.Core.Tests.Generation
{
    [TestFixture]
    public class DailyScheduleTests
    {
        [Test]
        public void A_given_date_always_maps_to_the_same_puzzle()
        {
            var date = new DateTime(2026, 8, 27);

            var first = DailySchedule.IndexFor(date, 1000);
            var second = DailySchedule.IndexFor(date, 1000);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void The_mapping_ignores_the_time_of_day()
        {
            var morning = new DateTime(2026, 8, 27, 6, 0, 0);
            var midnight = new DateTime(2026, 8, 27, 23, 59, 59);

            Assert.That(DailySchedule.IndexFor(midnight, 1000),
                Is.EqualTo(DailySchedule.IndexFor(morning, 1000)));
        }

        [Test]
        public void Indices_stay_inside_the_bank()
        {
            var date = new DateTime(2026, 1, 1);

            for (var day = 0; day < 800; day++)
            {
                var index = DailySchedule.IndexFor(date.AddDays(day), 365);
                Assert.That(index, Is.InRange(0, 364));
            }
        }

        [Test]
        public void Consecutive_days_do_not_repeat_the_same_puzzle()
        {
            var date = new DateTime(2026, 1, 1);
            var repeats = 0;

            for (var day = 0; day < 365; day++)
                if (DailySchedule.IndexFor(date.AddDays(day), 2000) ==
                    DailySchedule.IndexFor(date.AddDays(day + 1), 2000))
                    repeats++;

            Assert.That(repeats, Is.Zero, "back-to-back days landed on the same puzzle");
        }

        [Test]
        public void Dates_spread_across_the_whole_bank()
        {
            var date = new DateTime(2026, 1, 1);
            var seen = new System.Collections.Generic.HashSet<int>();

            for (var day = 0; day < 500; day++)
                seen.Add(DailySchedule.IndexFor(date.AddDays(day), 1000));

            // A poor hash would cluster; 500 draws from 1000 slots should leave
            // well over half of them distinct.
            Assert.That(seen.Count, Is.GreaterThan(350));
        }

        [Test]
        public void The_week_gets_harder_towards_the_weekend()
        {
            var monday = DailySchedule.TierFor(DayOfWeek.Monday);
            var wednesday = DailySchedule.TierFor(DayOfWeek.Wednesday);
            var sunday = DailySchedule.TierFor(DayOfWeek.Sunday);

            Assert.That(monday, Is.LessThan(wednesday));
            Assert.That(wednesday, Is.LessThan(sunday));
            Assert.That(monday, Is.EqualTo(DifficultyTier.Easy));
            Assert.That(sunday, Is.EqualTo(DifficultyTier.Master));
        }

        [Test]
        public void Every_weekday_has_a_tier()
        {
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                Assert.That(Enum.IsDefined(typeof(DifficultyTier), DailySchedule.TierFor(day)), Is.True);
        }
    }
}
