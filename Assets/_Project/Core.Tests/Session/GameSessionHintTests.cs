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

        [Test]
        public void Taking_a_hint_honours_the_same_preference_as_peeking_at_one()
        {
            var session = NewSession();
            var peeked = session.PeekHint(preferredCell: 2);

            session.UseHint(preferredCell: 2);

            Assert.That(session.ValueAt(2), Is.EqualTo(peeked.Digit),
                "peek and use must agree, or the UI highlights one cell and fills another");
        }

        [Test]
        public void Revealing_a_hint_fills_nothing_and_spends_nothing()
        {
            var session = NewSession();
            var before = session.HintsRemaining;

            var revealed = session.RevealHint();

            Assert.That(revealed, Is.Not.Null);
            Assert.That(session.ValueAt(revealed.CellIndex), Is.EqualTo(Board.Empty),
                "the first tap teaches; it does not answer");
            Assert.That(session.HintsRemaining, Is.EqualTo(before));
            Assert.That(session.HintsUsed, Is.Zero);
        }

        [Test]
        public void Taking_a_revealed_hint_spends_exactly_one()
        {
            var session = NewSession();
            var before = session.HintsRemaining;
            session.RevealHint();

            var taken = session.TakeHint();

            Assert.That(taken, Is.True);
            Assert.That(session.HintsRemaining, Is.EqualTo(before - 1));
            Assert.That(session.HintsUsed, Is.EqualTo(1));
        }

        [Test]
        public void The_cell_that_was_revealed_is_the_cell_that_gets_filled()
        {
            var session = NewSession();
            var revealed = session.RevealHint(preferredCell: 2);

            session.TakeHint();

            Assert.That(session.ValueAt(revealed.CellIndex), Is.EqualTo(revealed.Digit));
            Assert.That(session.PendingHint, Is.Null, "taking the offer withdraws it");
        }

        [Test]
        public void Revealing_twice_re_offers_the_same_hint()
        {
            var session = NewSession();

            var first = session.RevealHint();
            var second = session.RevealHint(preferredCell: 2);

            Assert.That(second, Is.SameAs(first),
                "a repeated tap must not swap the cell out from under the player");
        }

        [Test]
        public void Cancelling_a_revealed_hint_leaves_it_unspent()
        {
            var session = NewSession();
            var before = session.HintsRemaining;
            var revealed = session.RevealHint();

            session.CancelHint();

            Assert.That(session.PendingHint, Is.Null);
            Assert.That(session.ValueAt(revealed.CellIndex), Is.EqualTo(Board.Empty));
            Assert.That(session.HintsRemaining, Is.EqualTo(before),
                "a hint the player walked away from was never taken");
        }

        [Test]
        public void A_tap_after_a_cancelled_hint_takes_nothing()
        {
            var session = NewSession();
            var before = session.HintsRemaining;
            session.RevealHint();
            session.CancelHint();

            Assert.That(session.TakeHint(), Is.False, "there is no offer left to accept");
            Assert.That(session.HintsRemaining, Is.EqualTo(before));
        }

        [Test]
        public void Playing_a_move_drops_a_revealed_hint_rather_than_letting_it_go_stale()
        {
            var session = NewSession();
            session.RevealHint();

            // Cell 2 (row 0, col 2) is empty in the classic puzzle.
            session.Place(2, KnownPuzzles.ClassicSolution[2] - '0');

            Assert.That(session.PendingHint, Is.Null);
        }

        [Test]
        public void Undoing_drops_a_revealed_hint_too()
        {
            var session = NewSession();
            session.Place(2, KnownPuzzles.ClassicSolution[2] - '0');
            session.RevealHint();

            session.Undo();

            Assert.That(session.PendingHint, Is.Null,
                "the board the deduction was made against has moved");
        }

        [Test]
        public void Nothing_is_spent_when_there_is_nothing_to_reveal()
        {
            var rules = RulesConfig.Default;
            rules.Hints = 0;
            var session = NewSession(rules);

            Assert.That(session.RevealHint(), Is.Null);
            Assert.That(session.PendingHint, Is.Null);
            Assert.That(session.TakeHint(), Is.False, "a second tap on nothing must not conjure a hint");
            Assert.That(session.HintsRemaining, Is.Zero);
        }
    }
}
