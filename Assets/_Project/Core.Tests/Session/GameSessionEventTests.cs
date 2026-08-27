using System.Collections.Generic;
using NUnit.Framework;
using Sudoku.Core.Model;
using Sudoku.Core.Session;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Session
{
    [TestFixture]
    public class GameSessionEventTests
    {
        static GameSession NewSession(RulesConfig rules = null) =>
            new GameSession(
                Puzzle.FromStrings(KnownPuzzles.ClassicClues, KnownPuzzles.ClassicSolution),
                rules ?? RulesConfig.Default);

        static List<GameEvent> Record(GameSession session)
        {
            var events = new List<GameEvent>();
            session.Emitted += events.Add;
            return events;
        }

        static bool Has(List<GameEvent> events, GameEventKind kind)
        {
            foreach (var e in events) if (e.Kind == kind) return true;
            return false;
        }

        static GameEvent First(List<GameEvent> events, GameEventKind kind)
        {
            foreach (var e in events) if (e.Kind == kind) return e;
            Assert.Fail($"no {kind} event was emitted");
            return default;
        }

        [Test]
        public void Starting_a_puzzle_is_announced()
        {
            var session = NewSession();
            var events = Record(session);

            session.Start();

            Assert.That(Has(events, GameEventKind.PuzzleStarted), Is.True);
        }

        [Test]
        public void A_correct_placement_is_announced_as_correct()
        {
            var session = NewSession();
            var events = Record(session);

            session.Place(2, 4);

            var placed = First(events, GameEventKind.CellPlaced);
            Assert.That(placed.CellIndex, Is.EqualTo(2));
            Assert.That(placed.Digit, Is.EqualTo(4));
            Assert.That(placed.WasCorrect, Is.True);
            Assert.That(Has(events, GameEventKind.MistakeMade), Is.False);
        }

        [Test]
        public void A_wrong_placement_reports_both_the_placement_and_the_mistake()
        {
            var session = NewSession();
            var events = Record(session);

            session.Place(2, 7);

            Assert.That(First(events, GameEventKind.CellPlaced).WasCorrect, Is.False);

            var mistake = First(events, GameEventKind.MistakeMade);
            Assert.That(mistake.MistakeCount, Is.EqualTo(1));
            Assert.That(mistake.HeartsRemaining, Is.EqualTo(session.HeartsRemaining));
        }

        [Test]
        public void Running_out_of_hearts_is_announced()
        {
            var rules = RulesConfig.Default;
            rules.Hearts = 1;
            var session = NewSession(rules);
            var events = Record(session);

            session.Place(2, 7);

            Assert.That(Has(events, GameEventKind.HeartsDepleted), Is.True);
        }

        [Test]
        public void Notes_undo_and_hints_are_each_announced()
        {
            var session = NewSession();
            var events = Record(session);

            session.ToggleNote(2, 6);
            session.Undo();
            session.UseHint();

            Assert.That(Has(events, GameEventKind.NoteToggled), Is.True);
            Assert.That(Has(events, GameEventKind.UndoUsed), Is.True);

            var hint = First(events, GameEventKind.HintUsed);
            Assert.That(hint.HintsRemaining, Is.EqualTo(session.HintsRemaining));
        }

        [Test]
        public void Completing_a_puzzle_reports_how_it_went()
        {
            var session = NewSession();
            var events = Record(session);
            session.Tick(90f);

            for (var i = 0; i < Board.CellCount; i++)
                if (session.ValueAt(i) == Board.Empty)
                    session.Place(i, KnownPuzzles.ClassicSolution[i] - '0');

            var completed = First(events, GameEventKind.PuzzleCompleted);
            Assert.That(completed.ElapsedSeconds, Is.EqualTo(90f).Within(0.001f));
            Assert.That(completed.MistakeCount, Is.Zero);
            Assert.That(completed.HintsUsed, Is.Zero);
        }

        [Test]
        public void Abandoning_a_puzzle_reports_how_far_the_player_got()
        {
            var session = NewSession();
            var events = Record(session);
            session.Tick(30f);
            session.Place(2, 4);

            session.Abandon();

            var abandoned = First(events, GameEventKind.PuzzleAbandoned);
            Assert.That(abandoned.ElapsedSeconds, Is.EqualTo(30f).Within(0.001f));
            Assert.That(abandoned.EmptyCellCount, Is.EqualTo(session.EmptyCellCount));
        }

        [Test]
        public void Abandoning_a_puzzle_reports_how_much_of_the_work_was_the_players()
        {
            var session = NewSession();
            var events = Record(session);

            // Row 0 of the classic puzzle is "530070000" and its solution is
            // "534678912", so these two cells are empty and these are right.
            session.Place(2, 4);
            session.Place(3, 6);
            session.Abandon();

            var abandoned = First(events, GameEventKind.PuzzleAbandoned);
            Assert.That(abandoned.FilledCellCount, Is.EqualTo(2),
                "the clues the puzzle was dealt with are not progress the player made");
        }

        [Test]
        public void Taking_a_digit_back_takes_the_progress_back_with_it()
        {
            var session = NewSession();
            var events = Record(session);

            session.Place(2, 4);
            session.Undo();

            Assert.That(First(events, GameEventKind.UndoUsed).FilledCellCount, Is.Zero);
        }

        [Test]
        public void A_rejected_move_announces_nothing()
        {
            var session = NewSession();
            var events = Record(session);

            session.Place(0, 9); // onto a clue

            Assert.That(events, Is.Empty);
        }

        [Test]
        public void A_session_with_no_listener_still_works()
        {
            var session = NewSession();

            Assert.DoesNotThrow(() => session.Place(2, 4));
        }
    }
}
