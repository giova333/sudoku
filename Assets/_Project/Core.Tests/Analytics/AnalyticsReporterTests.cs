using System.Collections.Generic;
using NUnit.Framework;
using Sudoku.Core.Analytics;
using Sudoku.Core.Model;
using Sudoku.Core.Session;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Analytics
{
    [TestFixture]
    public class AnalyticsReporterTests
    {
        /// <summary>
        /// Stands in for the SDK that is not bound yet. That this is all a
        /// backend has to implement is the point of the whole ticket.
        /// </summary>
        sealed class RecordingService : IAnalyticsService
        {
            public readonly List<AnalyticsEvent> Events = new List<AnalyticsEvent>();
            public int Flushes;

            public void Track(AnalyticsEvent recorded) => Events.Add(recorded);

            public void Flush() => Flushes++;
        }

        static GameSession NewSession(RulesConfig rules = null) =>
            new GameSession(
                Puzzle.FromStrings(KnownPuzzles.ClassicClues, KnownPuzzles.ClassicSolution),
                rules ?? RulesConfig.Default);

        static AnalyticsReporter Reporting(RecordingService service, GameSession session)
        {
            var reporter = new AnalyticsReporter(service);
            reporter.Context.SessionId = "run-1";
            reporter.Context.AppVersion = "0.1.0";
            reporter.Context.Platform = "OSXEditor";
            reporter.Context.Theme = "light";
            reporter.Context.Difficulty = "Hard";
            reporter.Context.Puzzle = "main-Hard#7";
            reporter.Observe(session);
            return reporter;
        }

        /// <summary>Fills <paramref name="count"/> empty cells with the digits the
        /// solution calls for, so nothing here is a mistake.</summary>
        static void PlaceCorrectly(GameSession session, int count)
        {
            var placed = 0;
            for (var i = 0; i < Board.CellCount && placed < count; i++)
                if (session.ValueAt(i) == Board.Empty &&
                    session.Place(i, KnownPuzzles.ClassicSolution[i] - '0'))
                    placed++;
        }

        /// <summary>The first empty cell, and a digit that is not the one it wants.</summary>
        static void PlaceWrongly(GameSession session)
        {
            for (var i = 0; i < Board.CellCount; i++)
                if (session.ValueAt(i) == Board.Empty)
                {
                    var solution = KnownPuzzles.ClassicSolution[i] - '0';
                    session.Place(i, solution == 9 ? 8 : solution + 1);
                    return;
                }
        }

        static List<string> NamesOf(RecordingService service)
        {
            var names = new List<string>();
            foreach (var e in service.Events) names.Add(e.Name);
            return names;
        }

        static int CountOf(RecordingService service, string name)
        {
            var n = 0;
            foreach (var e in service.Events) if (e.Name == name) n++;
            return n;
        }

        static AnalyticsEvent First(RecordingService service, string name)
        {
            foreach (var e in service.Events) if (e.Name == name) return e;
            Assert.Fail($"no {name} event was recorded; got {string.Join(", ", NamesOf(service))}");
            return default;
        }

        static AnalyticsEvent Last(RecordingService service, string name)
        {
            for (var i = service.Events.Count - 1; i >= 0; i--)
                if (service.Events[i].Name == name) return service.Events[i];

            Assert.Fail($"no {name} event was recorded; got {string.Join(", ", NamesOf(service))}");
            return default;
        }

        static double Number(AnalyticsEvent recorded, string key)
        {
            Assert.That(recorded.TryGet(key, out var parameter), Is.True,
                $"{recorded.Name} carries no {key}");
            Assert.That(parameter.IsNumber, Is.True, $"{key} should be a number");
            return parameter.Number;
        }

        static string Text(AnalyticsEvent recorded, string key)
        {
            Assert.That(recorded.TryGet(key, out var parameter), Is.True,
                $"{recorded.Name} carries no {key}");
            return parameter.Text;
        }

        [Test]
        public void Every_event_carries_the_common_parameters_without_the_call_site_supplying_them()
        {
            var service = new RecordingService();
            var session = NewSession();
            Reporting(service, session);

            session.Start();

            var started = First(service, "puzzle_started");
            Assert.That(Text(started, "session_id"), Is.EqualTo("run-1"));
            Assert.That(Text(started, "app_version"), Is.EqualTo("0.1.0"));
            Assert.That(Text(started, "platform"), Is.EqualTo("OSXEditor"));
            Assert.That(Text(started, "theme"), Is.EqualTo("light"));
            Assert.That(Text(started, "difficulty"), Is.EqualTo("Hard"));
            Assert.That(Text(started, "puzzle"), Is.EqualTo("main-Hard#7"));
        }

        [Test]
        public void A_screen_view_carries_the_same_common_parameters_as_a_move()
        {
            var service = new RecordingService();
            var session = NewSession();
            var reporter = Reporting(service, session);

            reporter.ScreenViewed("home");

            var viewed = First(service, "screen_viewed");
            Assert.That(Text(viewed, "screen"), Is.EqualTo("home"));
            Assert.That(Text(viewed, "app_version"), Is.EqualTo("0.1.0"));
        }

        [Test]
        public void A_parameter_with_nothing_in_it_is_left_off_rather_than_sent_blank()
        {
            var service = new RecordingService();
            var reporter = new AnalyticsReporter(service);
            reporter.Context.SessionId = "run-1";

            reporter.ScreenViewed("home");

            Assert.That(First(service, "screen_viewed").TryGet("difficulty", out _), Is.False,
                "an event outside a puzzle should not report an empty difficulty");
        }

        [Test]
        public void Changing_a_setting_is_reported()
        {
            var service = new RecordingService();
            var reporter = new AnalyticsReporter(service);

            reporter.SettingChanged("sound", "False");

            var changed = First(service, "setting_changed");
            Assert.That(Text(changed, "setting"), Is.EqualTo("sound"));
            Assert.That(Text(changed, "value"), Is.EqualTo("False"));
        }

        [Test]
        public void Placements_are_batched_rather_than_sent_one_per_placement()
        {
            var service = new RecordingService();
            var session = NewSession();
            Reporting(service, session);

            PlaceCorrectly(session, AnalyticsReporter.CellPlacementBatchSize);

            Assert.That(CountOf(service, "cell_placed"), Is.EqualTo(1),
                "a full batch of placements should be one event, not one each");

            var batch = First(service, "cell_placed");
            Assert.That(Number(batch, "count"),
                Is.EqualTo((double)AnalyticsReporter.CellPlacementBatchSize));
            Assert.That(Number(batch, "correct"),
                Is.EqualTo((double)AnalyticsReporter.CellPlacementBatchSize));
            Assert.That(Number(batch, "wrong"), Is.Zero);
        }

        [Test]
        public void A_part_full_batch_waits_rather_than_being_sent()
        {
            var service = new RecordingService();
            var session = NewSession();
            Reporting(service, session);

            PlaceCorrectly(session, 3);

            Assert.That(service.Events, Is.Empty);
        }

        [Test]
        public void Anything_that_is_not_a_placement_sends_the_batch_first_so_the_order_survives()
        {
            var service = new RecordingService();
            var session = NewSession();
            Reporting(service, session);

            PlaceCorrectly(session, 3);
            session.ToggleNote(2, 6);

            Assert.That(NamesOf(service), Is.EqualTo(new[] { "cell_placed", "note_toggled" }));
            Assert.That(Number(First(service, "cell_placed"), "count"), Is.EqualTo(3d));
        }

        [Test]
        public void A_batch_says_how_many_of_its_placements_were_wrong()
        {
            var service = new RecordingService();
            var session = NewSession();
            Reporting(service, session);

            PlaceCorrectly(session, 2);
            PlaceWrongly(session);

            var batch = First(service, "cell_placed");
            Assert.That(Number(batch, "count"), Is.EqualTo(3d));
            Assert.That(Number(batch, "correct"), Is.EqualTo(2d));
            Assert.That(Number(batch, "wrong"), Is.EqualTo(1d));
        }

        [Test]
        public void A_batch_describes_where_it_ended_and_not_where_the_one_before_it_did()
        {
            var service = new RecordingService();
            var session = NewSession();
            var reporter = Reporting(service, session);

            PlaceCorrectly(session, 3);
            reporter.Flush();
            PlaceCorrectly(session, 2);
            reporter.Flush();

            var batch = Last(service, "cell_placed");
            Assert.That(Number(batch, "count"), Is.EqualTo(2d));
            Assert.That(Number(batch, "filled_cells"), Is.EqualTo(5d),
                "a flushed batch must leave nothing of itself behind for the next one");
        }

        [Test]
        public void Flushing_hands_over_the_placements_in_hand_and_the_backend_buffer()
        {
            var service = new RecordingService();
            var session = NewSession();
            var reporter = Reporting(service, session);
            PlaceCorrectly(session, 3);

            reporter.Flush();

            Assert.That(Number(First(service, "cell_placed"), "count"), Is.EqualTo(3d));
            Assert.That(service.Flushes, Is.EqualTo(1));
        }

        [Test]
        public void Every_move_a_player_can_make_is_reported()
        {
            var rules = RulesConfig.Default;
            rules.Hearts = 1;
            var service = new RecordingService();
            var session = NewSession(rules);
            Reporting(service, session);

            session.Start();
            session.ToggleNote(2, 6);
            session.Undo();
            session.UseHint();
            PlaceWrongly(session);

            var names = NamesOf(service);
            Assert.That(names, Contains.Item("puzzle_started"));
            Assert.That(names, Contains.Item("note_toggled"));
            Assert.That(names, Contains.Item("undo_used"));
            Assert.That(names, Contains.Item("hint_used"));
            Assert.That(names, Contains.Item("cell_placed"));
            Assert.That(names, Contains.Item("mistake_made"));
            Assert.That(names, Contains.Item("hearts_depleted"));
        }

        [Test]
        public void Abandoning_a_puzzle_reports_the_time_spent_and_how_far_the_player_got()
        {
            var service = new RecordingService();
            var session = NewSession();
            Reporting(service, session);
            session.Start();
            session.Tick(30f);
            PlaceCorrectly(session, 4);

            session.Abandon();

            var abandoned = First(service, "puzzle_abandoned");
            Assert.That(Number(abandoned, "elapsed_seconds"), Is.EqualTo(30d).Within(0.001));
            Assert.That(Number(abandoned, "filled_cells"), Is.EqualTo(4d),
                "the four cells the player filled, not the clues they were given");
            Assert.That(Text(abandoned, "difficulty"), Is.EqualTo("Hard"),
                "abandonment is only a calibration signal if it names the tier");
        }

        [Test]
        public void Completing_a_puzzle_reports_how_it_went()
        {
            var service = new RecordingService();
            var session = NewSession();
            Reporting(service, session);
            session.Start();
            session.Tick(90f);

            PlaceCorrectly(session, Board.CellCount);

            var completed = First(service, "puzzle_completed");
            Assert.That(Number(completed, "elapsed_seconds"), Is.EqualTo(90d).Within(0.001));
            Assert.That(Number(completed, "mistake_count"), Is.Zero);
            Assert.That(Number(completed, "hints_used"), Is.Zero);
        }

        [Test]
        public void The_placements_that_finished_a_puzzle_are_reported_before_the_completion()
        {
            var service = new RecordingService();
            var session = NewSession();
            Reporting(service, session);

            PlaceCorrectly(session, Board.CellCount);

            var names = NamesOf(service);
            Assert.That(names[names.Count - 1], Is.EqualTo("puzzle_completed"));
            Assert.That(names[names.Count - 2], Is.EqualTo("cell_placed"),
                "the last few placements must not be stranded in the batch");
        }

        [Test]
        public void Taking_up_the_next_puzzle_stops_reporting_the_one_before_it()
        {
            var service = new RecordingService();
            var first = NewSession();
            var reporter = Reporting(service, first);
            var second = NewSession();

            reporter.Observe(second);
            first.ToggleNote(2, 6);

            Assert.That(CountOf(service, "note_toggled"), Is.Zero,
                "a session that was handed over should have no listener left on it");
        }

        [Test]
        public void Leaving_a_puzzle_mid_batch_does_not_lose_the_moves_in_it()
        {
            var service = new RecordingService();
            var first = NewSession();
            var reporter = Reporting(service, first);
            PlaceCorrectly(first, 3);

            reporter.Observe(NewSession());

            Assert.That(Number(First(service, "cell_placed"), "count"), Is.EqualTo(3d));
        }

        [Test]
        public void A_puzzle_dealt_next_is_reported_under_its_own_difficulty()
        {
            var service = new RecordingService();
            var session = NewSession();
            var reporter = Reporting(service, session);

            reporter.Context.Difficulty = "Easy";
            reporter.Context.Puzzle = "main-Easy#3";
            session.Start();

            var started = First(service, "puzzle_started");
            Assert.That(Text(started, "difficulty"), Is.EqualTo("Easy"));
            Assert.That(Text(started, "puzzle"), Is.EqualTo("main-Easy#3"));
        }
    }
}
