using System.Collections.Generic;
using NUnit.Framework;
using Sudoku.Core.Model;
using Sudoku.Core.Session;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Session
{
    /// <summary>
    /// Starting the same puzzle over, as the pause screen offers it. Everything
    /// the player accumulated during the run goes; the puzzle itself stays.
    /// </summary>
    [TestFixture]
    public class GameSessionRestartTests
    {
        static Puzzle ClassicPuzzle() => Puzzle.FromStrings(
            KnownPuzzles.ClassicClues,
            KnownPuzzles.ClassicSolution);

        static GameSession NewSession(RulesConfig rules = null) =>
            new GameSession(ClassicPuzzle(), rules ?? RulesConfig.Default);

        [Test]
        public void Restarting_clears_the_digits_the_player_entered()
        {
            var session = NewSession();
            session.Place(2, 4); // cell 2 is empty in the classic puzzle

            session.Restart();

            Assert.That(session.ValueAt(2), Is.EqualTo(Board.Empty));
        }

        [Test]
        public void Restarting_keeps_the_puzzles_clues()
        {
            var session = NewSession();
            session.Place(2, 4);

            session.Restart();

            // Row 0 of the classic puzzle is "530070000".
            Assert.That(session.ValueAt(0), Is.EqualTo(5));
            Assert.That(session.ValueAt(1), Is.EqualTo(3));
            Assert.That(session.ValueAt(4), Is.EqualTo(7));
        }

        [Test]
        public void Restarting_clears_the_players_notes()
        {
            var session = NewSession();
            session.ToggleNote(2, 6);

            session.Restart();

            Assert.That(session.HasNote(2, 6), Is.False);
        }

        [Test]
        public void Restarting_puts_every_empty_cell_back()
        {
            var session = NewSession();
            var before = session.EmptyCellCount;
            session.Place(2, 4);

            session.Restart();

            Assert.That(session.EmptyCellCount, Is.EqualTo(before));
        }

        [Test]
        public void Restarting_resets_the_timer()
        {
            var session = NewSession();
            session.Tick(90f);

            session.Restart();

            Assert.That(session.ElapsedSeconds, Is.Zero);
        }

        [Test]
        public void Restarting_gives_back_the_hearts_the_run_cost()
        {
            var session = NewSession();
            session.Place(2, 7); // wrong: cell 2 solves to 4

            session.Restart();

            Assert.That(session.HeartsRemaining, Is.EqualTo(RulesConfig.Default.Hearts));
        }

        [Test]
        public void Restarting_forgets_the_mistakes_of_the_run()
        {
            var session = NewSession();
            session.Place(2, 7);

            session.Restart();

            Assert.That(session.MistakeCount, Is.Zero);
        }

        [Test]
        public void Restarting_gives_back_the_hints_the_run_spent()
        {
            var session = NewSession();
            session.UseHint();

            session.Restart();

            Assert.That(session.HintsRemaining, Is.EqualTo(RulesConfig.Default.Hints));
            Assert.That(session.HintsUsed, Is.Zero);
        }

        [Test]
        public void Restarting_empties_the_undo_history()
        {
            var session = NewSession();
            session.Place(2, 4);
            session.Place(5, 8);

            session.Restart();

            Assert.That(session.UndoDepth, Is.Zero);
            Assert.That(session.Undo(), Is.False, "there is nothing before the first tap to undo back into");
        }

        [Test]
        public void Restarting_drops_a_hint_that_was_revealed_but_not_taken()
        {
            var session = NewSession();
            session.RevealHint();

            session.Restart();

            Assert.That(session.PendingHint, Is.Null);
        }

        [Test]
        public void A_puzzle_lost_to_the_heart_limit_can_be_restarted()
        {
            var rules = RulesConfig.Default;
            rules.Hearts = 1;
            var session = new GameSession(ClassicPuzzle(), rules);
            session.Place(2, 7); // burns the only heart

            session.Restart();

            Assert.That(session.Status, Is.EqualTo(SessionStatus.InProgress));
            Assert.That(session.Place(2, 4), Is.True, "a restarted puzzle accepts moves again");
        }

        [Test]
        public void Restarting_stocks_from_the_rules_as_they_stand_rather_than_as_they_were_dealt()
        {
            // The presenter writes a settings change into this same object
            // rather than over it, so a restart under a changed mistake limit
            // reaches the session in play. This is the mechanism that relies on.
            var rules = RulesConfig.Default;
            var session = NewSession(rules);

            rules.Hearts = 5;
            session.Restart();

            Assert.That(session.HeartsRemaining, Is.EqualTo(5));
        }

        [Test]
        public void A_mistake_limit_turned_off_before_a_restart_governs_the_new_run()
        {
            var rules = RulesConfig.Default;
            rules.Hearts = 1;
            var session = NewSession(rules);
            session.Place(2, 7); // burns the only heart: the run is over

            rules.MistakeLimitEnabled = false;
            session.Restart();
            session.Place(2, 7);

            Assert.That(session.Status, Is.EqualTo(SessionStatus.InProgress),
                "a run started over under no mistake limit cannot end out of hearts");
        }

        [Test]
        public void Restarting_leaves_a_paused_session_paused()
        {
            var session = NewSession();
            session.Pause();

            session.Restart();
            session.Tick(5f);

            Assert.That(session.ElapsedSeconds, Is.Zero);
        }

        [Test]
        public void Restarting_announces_that_the_puzzle_is_starting_again()
        {
            var session = NewSession();
            session.Start();

            var events = new List<GameEvent>();
            session.Emitted += events.Add;
            session.Restart();

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Kind, Is.EqualTo(GameEventKind.PuzzleStarted));
            Assert.That(events[0].ElapsedSeconds, Is.Zero, "the announced run starts from zero");
        }
    }
}
