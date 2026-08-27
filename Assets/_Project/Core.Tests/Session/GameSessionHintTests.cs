using NUnit.Framework;
using Sudoku.Core.Model;
using Sudoku.Core.Session;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Session
{
    [TestFixture]
    public class GameSessionHintTests
    {
        static GameSession NewSession(RulesConfig rules = null) =>
            new GameSession(
                Puzzle.FromStrings(KnownPuzzles.ClassicClues, KnownPuzzles.ClassicSolution),
                rules ?? RulesConfig.Default);

        [Test]
        public void A_hint_names_a_cell_and_the_digit_that_really_belongs_there()
        {
            var session = NewSession();

            var hint = session.PeekHint();

            Assert.That(hint, Is.Not.Null);
            Assert.That(session.ValueAt(hint.CellIndex), Is.EqualTo(Board.Empty), "hints target empty cells");
            Assert.That(hint.Digit, Is.EqualTo(KnownPuzzles.ClassicSolution[hint.CellIndex] - '0'));
        }

        [Test]
        public void A_hint_explains_itself_by_naming_the_cells_that_force_it()
        {
            var hint = NewSession().PeekHint();

            Assert.That(hint.ReasonCells, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Peeking_at_a_hint_does_not_spend_one()
        {
            var session = NewSession();
            var before = session.HintsRemaining;

            session.PeekHint();
            session.PeekHint();

            Assert.That(session.HintsRemaining, Is.EqualTo(before));
        }

        [Test]
        public void Taking_a_hint_fills_the_cell_and_spends_one()
        {
            var session = NewSession();
            var before = session.HintsRemaining;
            var hint = session.PeekHint();

            var taken = session.UseHint();

            Assert.That(taken, Is.True);
            Assert.That(session.ValueAt(hint.CellIndex), Is.EqualTo(hint.Digit));
            Assert.That(session.HintsRemaining, Is.EqualTo(before - 1));
            Assert.That(session.HintsUsed, Is.EqualTo(1));
        }

        [Test]
        public void A_hint_costs_no_heart_and_is_not_a_mistake()
        {
            var session = NewSession();
            var hearts = session.HeartsRemaining;

            session.UseHint();

            Assert.That(session.HeartsRemaining, Is.EqualTo(hearts));
            Assert.That(session.MistakeCount, Is.Zero);
        }

        [Test]
        public void Hints_run_out()
        {
            var rules = RulesConfig.Default;
            rules.Hints = 2;
            var session = NewSession(rules);

            Assert.That(session.UseHint(), Is.True);
            Assert.That(session.UseHint(), Is.True);

            Assert.That(session.UseHint(), Is.False, "a third hint should not be available");
            Assert.That(session.PeekHint(), Is.Null);
            Assert.That(session.HintsRemaining, Is.Zero);
        }

        [Test]
        public void A_hint_prefers_the_cell_the_player_is_looking_at()
        {
            var session = NewSession();

            // Cell 2 is empty and solvable in the classic puzzle.
            var hint = session.PeekHint(preferredCell: 2);

            Assert.That(hint.CellIndex, Is.EqualTo(2));
            Assert.That(hint.Digit, Is.EqualTo(KnownPuzzles.ClassicSolution[2] - '0'));
        }

        [Test]
        public void A_hint_is_never_spent_on_a_cell_that_is_already_right()
        {
            var session = NewSession();
            session.Place(2, KnownPuzzles.ClassicSolution[2] - '0');

            var hint = session.PeekHint(preferredCell: 2);

            Assert.That(hint.CellIndex, Is.Not.EqualTo(2), "that cell needs no help");
        }

        [Test]
        public void A_hint_can_be_undone_but_is_not_refunded()
        {
            var session = NewSession();
            var hint = session.PeekHint();
            session.UseHint();

            session.Undo();

            Assert.That(session.ValueAt(hint.CellIndex), Is.EqualTo(Board.Empty));
            Assert.That(session.HintsRemaining, Is.EqualTo(RulesConfig.Default.Hints - 1),
                "undo gives back the move, never the resource");
            Assert.That(session.HintsUsed, Is.EqualTo(1));
        }

        [Test]
        public void A_finished_session_offers_no_hints()
        {
            var rules = RulesConfig.Default;
            rules.Hearts = 1;
            var session = NewSession(rules);
            session.Place(2, 7); // wrong; burns the only heart

            Assert.That(session.PeekHint(), Is.Null);
            Assert.That(session.UseHint(), Is.False);
        }
    }
}
