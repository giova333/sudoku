using NUnit.Framework;
using Sudoku.Core.Model;
using Sudoku.Core.Session;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Session
{
    [TestFixture]
    public class GameSessionTests
    {
        static GameSession NewClassicSession()
        {
            var puzzle = Puzzle.FromStrings(
                KnownPuzzles.ClassicClues,
                KnownPuzzles.ClassicSolution);
            return new GameSession(puzzle);
        }

        [Test]
        public void A_new_session_shows_the_puzzles_clues_on_the_board()
        {
            var session = NewClassicSession();

            // Row 0 of the classic puzzle is "530070000".
            Assert.That(session.ValueAt(0), Is.EqualTo(5));
            Assert.That(session.ValueAt(1), Is.EqualTo(3));
            Assert.That(session.ValueAt(2), Is.EqualTo(Board.Empty));
            Assert.That(session.ValueAt(4), Is.EqualTo(7));
        }

        [Test]
        public void Placing_a_digit_in_an_empty_cell_shows_it_on_the_board()
        {
            var session = NewClassicSession();

            // Cell 2 (row 0, col 2) is empty in the classic puzzle.
            session.Place(2, 4);

            Assert.That(session.ValueAt(2), Is.EqualTo(4));
        }

        [Test]
        public void A_clue_cannot_be_overwritten_by_the_player()
        {
            var session = NewClassicSession();

            // Cell 0 is the clue '5'.
            var accepted = session.Place(0, 9);

            Assert.That(accepted, Is.False, "placing onto a clue should be rejected");
            Assert.That(session.ValueAt(0), Is.EqualTo(5), "the clue must be untouched");
        }

        [Test]
        public void A_digit_that_differs_from_the_solution_is_flagged_as_a_mistake()
        {
            var session = NewClassicSession();

            // Cell 2 solves to 4 in the classic puzzle.
            session.Place(2, 7);

            Assert.That(session.ValueAt(2), Is.EqualTo(7), "a wrong digit stays on the board");
            Assert.That(session.IsMistakeAt(2), Is.True);
        }

        [Test]
        public void A_digit_matching_the_solution_is_not_flagged()
        {
            var session = NewClassicSession();

            session.Place(2, 4);

            Assert.That(session.IsMistakeAt(2), Is.False);
        }

        [Test]
        public void A_wrong_placement_costs_a_heart()
        {
            var session = NewClassicSession();
            var before = session.HeartsRemaining;

            session.Place(2, 7); // cell 2 solves to 4

            Assert.That(session.HeartsRemaining, Is.EqualTo(before - 1));
        }

        [Test]
        public void A_correct_placement_costs_no_heart()
        {
            var session = NewClassicSession();
            var before = session.HeartsRemaining;

            session.Place(2, 4);

            Assert.That(session.HeartsRemaining, Is.EqualTo(before));
        }

        [Test]
        public void A_note_can_be_toggled_on_and_off()
        {
            var session = NewClassicSession();

            session.ToggleNote(2, 6);
            Assert.That(session.HasNote(2, 6), Is.True);

            session.ToggleNote(2, 6);
            Assert.That(session.HasNote(2, 6), Is.False);
        }

        [Test]
        public void A_cell_holds_several_notes_at_once()
        {
            var session = NewClassicSession();

            session.ToggleNote(2, 1);
            session.ToggleNote(2, 4);
            session.ToggleNote(2, 9);

            Assert.That(session.HasNote(2, 1), Is.True);
            Assert.That(session.HasNote(2, 4), Is.True);
            Assert.That(session.HasNote(2, 9), Is.True);
            Assert.That(session.HasNote(2, 5), Is.False);
        }

        [Test]
        public void Noting_a_wrong_digit_never_costs_a_heart()
        {
            var session = NewClassicSession();
            var before = session.HeartsRemaining;

            session.ToggleNote(2, 7); // cell 2 solves to 4

            Assert.That(session.HeartsRemaining, Is.EqualTo(before));
            Assert.That(session.MistakeCount, Is.Zero);
        }

        [Test]
        public void Notes_cannot_be_placed_on_a_clue()
        {
            var session = NewClassicSession();

            var accepted = session.ToggleNote(0, 7);

            Assert.That(accepted, Is.False);
            Assert.That(session.HasNote(0, 7), Is.False);
        }

        [Test]
        public void Placing_a_digit_clears_that_cells_own_notes()
        {
            var session = NewClassicSession();
            session.ToggleNote(2, 1);
            session.ToggleNote(2, 4);

            session.Place(2, 4);

            Assert.That(session.HasNote(2, 1), Is.False);
            Assert.That(session.HasNote(2, 4), Is.False);
        }

        [Test]
        public void Re_entering_the_identical_wrong_digit_costs_nothing()
        {
            var session = NewClassicSession();

            session.Place(2, 7);
            var afterFirst = session.HeartsRemaining;
            session.Place(2, 7); // fat-fingered double tap

            Assert.That(session.HeartsRemaining, Is.EqualTo(afterFirst));
        }

        [Test]
        public void A_different_wrong_digit_in_the_same_cell_costs_another_heart()
        {
            var session = NewClassicSession();

            session.Place(2, 7);
            var afterFirst = session.HeartsRemaining;
            session.Place(2, 8);

            Assert.That(session.HeartsRemaining, Is.EqualTo(afterFirst - 1));
        }

        [Test]
        public void Erasing_clears_a_players_digit()
        {
            var session = NewClassicSession();
            session.Place(2, 4);

            session.Erase(2);

            Assert.That(session.ValueAt(2), Is.EqualTo(Board.Empty));
        }

        [Test]
        public void Erasing_a_clue_does_nothing()
        {
            var session = NewClassicSession();

            var accepted = session.Erase(0);

            Assert.That(accepted, Is.False);
            Assert.That(session.ValueAt(0), Is.EqualTo(5));
        }

        [Test]
        public void Erasing_costs_no_heart_even_after_a_mistake()
        {
            var session = NewClassicSession();
            session.Place(2, 7);
            var before = session.HeartsRemaining;

            session.Erase(2);

            Assert.That(session.HeartsRemaining, Is.EqualTo(before));
        }

        [Test]
        public void Placing_a_digit_removes_that_digit_from_peer_notes()
        {
            var session = NewClassicSession();
            // Cell 2 is row 0, column 2, box 0.
            session.ToggleNote(3, 4);   // same row
            session.ToggleNote(11, 4);  // same column and box
            session.ToggleNote(40, 4);  // unrelated cell

            session.Place(2, 4);

            Assert.That(session.HasNote(3, 4), Is.False, "row peer");
            Assert.That(session.HasNote(11, 4), Is.False, "column/box peer");
            Assert.That(session.HasNote(40, 4), Is.True, "non-peer must be untouched");
        }

        [Test]
        public void Placing_a_digit_leaves_other_digits_notes_alone()
        {
            var session = NewClassicSession();
            session.ToggleNote(3, 4);
            session.ToggleNote(3, 9);

            session.Place(2, 4);

            Assert.That(session.HasNote(3, 4), Is.False);
            Assert.That(session.HasNote(3, 9), Is.True);
        }

        [Test]
        public void Peer_note_removal_can_be_switched_off()
        {
            var puzzle = Puzzle.FromStrings(KnownPuzzles.ClassicClues, KnownPuzzles.ClassicSolution);
            var rules = RulesConfig.Default;
            rules.AutoRemoveNotes = false;
            var session = new GameSession(puzzle, rules);
            session.ToggleNote(3, 4);

            session.Place(2, 4);

            Assert.That(session.HasNote(3, 4), Is.True);
        }


        [Test]
        public void Undo_reverses_a_placement()
        {
            var session = NewClassicSession();
            session.Place(2, 4);

            session.Undo();

            Assert.That(session.ValueAt(2), Is.EqualTo(Board.Empty));
        }

        [Test]
        public void Undo_restores_peer_notes_removed_by_a_placement_in_one_step()
        {
            var session = NewClassicSession();
            session.ToggleNote(3, 4);
            session.ToggleNote(11, 4);

            session.Place(2, 4);
            session.Undo();

            Assert.That(session.ValueAt(2), Is.EqualTo(Board.Empty), "the placement is reversed");
            Assert.That(session.HasNote(3, 4), Is.True, "and so are its note removals");
            Assert.That(session.HasNote(11, 4), Is.True);
        }

        [Test]
        public void Undo_restores_the_cells_own_notes_cleared_by_a_placement()
        {
            var session = NewClassicSession();
            session.ToggleNote(2, 1);
            session.ToggleNote(2, 9);

            session.Place(2, 4);
            session.Undo();

            Assert.That(session.HasNote(2, 1), Is.True);
            Assert.That(session.HasNote(2, 9), Is.True);
        }

        [Test]
        public void Undo_reverses_a_note_toggle()
        {
            var session = NewClassicSession();
            session.ToggleNote(2, 6);

            session.Undo();

            Assert.That(session.HasNote(2, 6), Is.False);
        }

        [Test]
        public void Undo_reverses_an_erase()
        {
            var session = NewClassicSession();
            session.Place(2, 4);
            session.Erase(2);

            session.Undo();

            Assert.That(session.ValueAt(2), Is.EqualTo(4));
        }

        [Test]
        public void Undo_does_not_refund_a_heart()
        {
            var session = NewClassicSession();
            session.Place(2, 7); // wrong
            var afterMistake = session.HeartsRemaining;

            session.Undo();

            Assert.That(session.HeartsRemaining, Is.EqualTo(afterMistake));
            Assert.That(session.MistakeCount, Is.EqualTo(1), "the mistake still happened");
        }

        [Test]
        public void Undo_with_nothing_to_undo_is_harmless()
        {
            var session = NewClassicSession();

            Assert.That(session.Undo(), Is.False);
            Assert.That(session.ValueAt(0), Is.EqualTo(5));
        }

        [Test]
        public void Undo_walks_back_through_the_whole_history()
        {
            var session = NewClassicSession();
            session.Place(2, 4);
            session.Place(3, 6);
            session.Place(5, 8);

            session.Undo();
            session.Undo();
            session.Undo();

            Assert.That(session.ValueAt(2), Is.EqualTo(Board.Empty));
            Assert.That(session.ValueAt(3), Is.EqualTo(Board.Empty));
            Assert.That(session.ValueAt(5), Is.EqualTo(Board.Empty));
        }

        [Test]
        public void A_rejected_move_leaves_nothing_to_undo()
        {
            var session = NewClassicSession();

            session.Place(0, 9); // onto a clue - rejected

            Assert.That(session.Undo(), Is.False);
        }

    }
}
