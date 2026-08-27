using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sudoku.Core.Commands;
using Sudoku.Core.Model;
using Sudoku.Core.Session;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Session
{
    [TestFixture]
    public class SessionRestoreTests
    {
        static Puzzle ClassicPuzzle() =>
            Puzzle.FromStrings(KnownPuzzles.ClassicClues, KnownPuzzles.ClassicSolution);

        static GameSession NewClassicSession()
        {
            var session = new GameSession(ClassicPuzzle(), RulesConfig.Default);
            session.Start();
            return session;
        }

        static GameSession Resumed(GameSession session) =>
            GameSession.Restore(ClassicPuzzle(), RulesConfig.Default, session.Capture());

        [Test]
        public void A_resumed_puzzle_is_not_announced_as_a_second_start()
        {
            var resumed = Resumed(NewClassicSession());

            var started = false;
            resumed.Emitted += e => started |= e.Kind == GameEventKind.PuzzleStarted;
            resumed.Start();

            Assert.That(started, Is.False, "a resume is not a new play-through");
        }

        [Test]
        public void A_resumed_puzzle_carries_on_accepting_moves_where_the_player_left_off()
        {
            var session = NewClassicSession();
            // Row 0 of the classic puzzle is "530070000", so cell 2 is empty.
            session.Place(2, 4);

            var resumed = Resumed(session);
            resumed.Place(3, 6);

            Assert.That(resumed.ValueAt(3), Is.EqualTo(6));
        }

        [Test]
        public void A_resumed_puzzle_can_undo_a_move_made_before_the_interruption()
        {
            var session = NewClassicSession();
            session.Place(2, 4);

            var resumed = Resumed(session);
            resumed.Undo();

            Assert.That(resumed.ValueAt(2), Is.EqualTo(Board.Empty));
        }

        [Test]
        public void A_composite_move_still_undoes_atomically_after_a_resume()
        {
            var session = NewClassicSession();
            session.ToggleNote(3, 4);
            session.Place(2, 4);

            var resumed = Resumed(session);
            resumed.Undo();

            // Placing the 4 struck it from cell 3's notes; one undo puts both back.
            Assert.That(resumed.HasNote(3, 4), Is.True);
        }

        [Test]
        public void A_puzzle_the_player_lost_is_still_lost_after_a_resume()
        {
            var rules = RulesConfig.Default;
            rules.Hearts = 1;

            var session = new GameSession(ClassicPuzzle(), rules);
            session.Start();
            session.Place(2, 9);

            var resumed = GameSession.Restore(ClassicPuzzle(), rules, session.Capture());

            Assert.That(resumed.Status, Is.EqualTo(SessionStatus.Failed));
        }

        [Test]
        public void A_snapshot_is_not_disturbed_by_the_moves_that_follow_it()
        {
            var session = NewClassicSession();
            var snapshot = session.Capture();

            session.Place(2, 4);

            Assert.That(snapshot.Values[2], Is.EqualTo(Board.Empty),
                "a snapshot handed to a writer must not change under it");
        }

        [Test]
        public void A_snapshot_taken_before_the_first_move_restores_the_puzzles_clues()
        {
            var resumed = Resumed(NewClassicSession());

            Assert.That(resumed.ValueAt(0), Is.EqualTo(5));
        }

        [Test]
        public void A_saved_undo_stack_deeper_than_the_limit_is_trimmed_on_restore()
        {
            var session = NewClassicSession();
            var snapshot = session.Capture();
            for (var i = 0; i < GameSession.UndoHistoryLimit + 50; i++)
                snapshot.History.Add(new BoardCommand(BoardCommandKind.ToggleNote, 2,
                    new List<BoardEdit> { new BoardEdit(2, Board.Empty, Board.Empty, 0, 1) }));

            var resumed = GameSession.Restore(ClassicPuzzle(), RulesConfig.Default, snapshot);

            Assert.That(resumed.UndoDepth, Is.EqualTo(GameSession.UndoHistoryLimit));
        }

        [Test]
        public void A_snapshot_that_is_not_a_whole_board_is_rejected()
        {
            var snapshot = NewClassicSession().Capture();
            snapshot.Values = new int[10];

            Assert.Throws<ArgumentException>(
                () => GameSession.Restore(ClassicPuzzle(), RulesConfig.Default, snapshot));
        }

        [Test]
        public void The_pencil_mark_mask_reports_every_digit_noted_in_a_cell()
        {
            var session = NewClassicSession();
            session.ToggleNote(2, 1);
            session.ToggleNote(2, 3);

            Assert.That(session.NotesAt(2), Is.EqualTo(0b000000101));
        }
    }
}
