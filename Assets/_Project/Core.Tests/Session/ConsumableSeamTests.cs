using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sudoku.Core.Model;
using Sudoku.Core.Session;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Session
{
    /// <summary>
    /// Hearts and hints are spent exclusively through
    /// <see cref="IConsumableService"/>. That is the seam rewarded ads and IAP
    /// bundles occupy later, so it is only worth anything if gameplay cannot
    /// route around it - and the way to prove that is to hand the session a
    /// service that says no and watch nothing happen.
    /// </summary>
    [TestFixture]
    public class ConsumableSeamTests
    {
        /// <summary>
        /// A service that records what it was asked for and can be told to
        /// refuse. Refusing is the interesting case: a session that decremented
        /// a counter of its own would carry on regardless, and this is what
        /// catches it.
        /// </summary>
        sealed class RecordingConsumables : IConsumableService
        {
            readonly int[] _balances = new int[2];

            public event Action<Consumable> Changed;

            public readonly List<Consumable> Spent = new List<Consumable>();
            public int RefillsAsked;
            public bool RefuseSpends;
            public bool Supplies;

            public int Remaining(Consumable consumable) => _balances[(int)consumable];

            public bool CanSpend(Consumable consumable) =>
                !RefuseSpends && Remaining(consumable) > 0;

            public bool Spend(Consumable consumable)
            {
                Spent.Add(consumable);
                if (!CanSpend(consumable))
                    return false;

                _balances[(int)consumable]--;
                Changed?.Invoke(consumable);
                return true;
            }

            public void Reset(Consumable consumable, int amount)
            {
                _balances[(int)consumable] = amount < 0 ? 0 : amount;
                Changed?.Invoke(consumable);
            }

            public bool CanRefill(Consumable consumable) => Supplies;

            public int Refill(Consumable consumable, int amount)
            {
                RefillsAsked++;
                if (!Supplies)
                    return 0;

                _balances[(int)consumable] += amount;
                Changed?.Invoke(consumable);
                return amount;
            }
        }

        static Puzzle ClassicPuzzle() => Puzzle.FromStrings(
            KnownPuzzles.ClassicClues,
            KnownPuzzles.ClassicSolution);

        static GameSession SessionWith(RecordingConsumables consumables, RulesConfig rules = null) =>
            new GameSession(ClassicPuzzle(), rules ?? RulesConfig.Default,
                ConstraintSet.Classic, consumables);

        /// <summary>Cell 2 is empty in the classic grid and its solution is 4.</summary>
        const int EmptyCell = 2;
        const int WrongDigit = 9;

        [Test]
        public void Dealing_a_puzzle_stocks_it_through_the_service()
        {
            var consumables = new RecordingConsumables();
            SessionWith(consumables);

            Assert.That(consumables.Remaining(Consumable.Heart), Is.EqualTo(3), "hearts");
            Assert.That(consumables.Remaining(Consumable.Hint), Is.EqualTo(3), "hints");
        }

        [Test]
        public void A_wrong_placement_asks_the_service_for_the_heart()
        {
            var consumables = new RecordingConsumables();
            var session = SessionWith(consumables);

            session.Place(EmptyCell, WrongDigit);

            Assert.That(consumables.Spent, Is.EqualTo(new[] { Consumable.Heart }));
            Assert.That(session.HeartsRemaining, Is.EqualTo(2));
        }

        [Test]
        public void A_correct_placement_asks_for_nothing()
        {
            var consumables = new RecordingConsumables();
            var session = SessionWith(consumables);

            session.Place(EmptyCell, 4);

            Assert.That(consumables.Spent, Is.Empty);
        }

        [Test]
        public void A_service_that_refuses_the_heart_keeps_it()
        {
            var consumables = new RecordingConsumables { RefuseSpends = true };
            var session = SessionWith(consumables);

            session.Place(EmptyCell, WrongDigit);

            Assert.That(session.HeartsRemaining, Is.EqualTo(3),
                "the session must not decrement a heart the service refused to spend");
            Assert.That(session.MistakeCount, Is.EqualTo(1), "the mistake still happened");
        }

        [Test]
        public void A_run_whose_hearts_are_never_spent_never_ends()
        {
            var consumables = new RecordingConsumables { RefuseSpends = true };
            var session = SessionWith(consumables);

            for (var i = 0; i < 5; i++)
            {
                session.Place(EmptyCell, WrongDigit);
                session.Erase(EmptyCell);
            }

            Assert.That(session.Status, Is.EqualTo(SessionStatus.InProgress));
        }

        [Test]
        public void Hearts_are_the_services_number_rather_than_a_copy_of_it()
        {
            var consumables = new RecordingConsumables();
            var session = SessionWith(consumables);

            // Moved behind the session's back. A session holding its own count
            // would still be reporting three.
            consumables.Reset(Consumable.Heart, 1);

            Assert.That(session.HeartsRemaining, Is.EqualTo(1));
        }

        [Test]
        public void Hints_are_the_services_number_rather_than_a_copy_of_it()
        {
            var consumables = new RecordingConsumables();
            var session = SessionWith(consumables);

            consumables.Reset(Consumable.Hint, 0);

            Assert.That(session.HintsRemaining, Is.Zero);
        }

        [Test]
        public void Taking_a_hint_asks_the_service_for_it()
        {
            var consumables = new RecordingConsumables();
            var session = SessionWith(consumables);

            session.RevealHint();
            session.TakeHint();

            Assert.That(consumables.Spent, Is.EqualTo(new[] { Consumable.Hint }));
            Assert.That(session.HintsRemaining, Is.EqualTo(2));
        }

        [Test]
        public void A_service_that_refuses_the_hint_leaves_the_board_alone()
        {
            var consumables = new RecordingConsumables { RefuseSpends = true };
            var session = SessionWith(consumables);

            var revealed = session.RevealHint();
            var taken = session.TakeHint();

            Assert.That(taken, Is.False, "a hint nobody would sell cannot be taken");
            Assert.That(session.ValueAt(revealed.CellIndex), Is.EqualTo(Board.Empty),
                "the hinted cell must stay empty when the hint was not paid for");
            Assert.That(session.HintsUsed, Is.Zero);
        }

        [Test]
        public void Restarting_a_puzzle_restocks_it_through_the_service()
        {
            var consumables = new RecordingConsumables();
            var session = SessionWith(consumables);

            session.Place(EmptyCell, WrongDigit);
            session.Restart();

            Assert.That(consumables.Remaining(Consumable.Heart), Is.EqualTo(3));
            Assert.That(session.HeartsRemaining, Is.EqualTo(3));
        }

        [Test]
        public void Restoring_a_saved_puzzle_puts_its_counters_back_through_the_service()
        {
            var consumables = new RecordingConsumables();
            var played = SessionWith(consumables);
            played.Place(EmptyCell, WrongDigit);

            var restored = GameSession.Restore(ClassicPuzzle(), RulesConfig.Default,
                played.Capture());

            Assert.That(restored.HeartsRemaining, Is.EqualTo(2));
        }

        [Test]
        public void Undo_never_asks_for_a_refund()
        {
            var consumables = new RecordingConsumables();
            var session = SessionWith(consumables);

            session.Place(EmptyCell, WrongDigit);
            session.Undo();

            Assert.That(consumables.RefillsAsked, Is.Zero,
                "undo must not hand a heart back - that is what would make the mistake economy decorative");
            Assert.That(session.HeartsRemaining, Is.EqualTo(2));
        }

        [Test]
        public void Nothing_supplies_hearts_in_this_milestone()
        {
            var consumables = new LocalConsumables();

            Assert.That(consumables.CanRefill(Consumable.Heart), Is.False, "availability");
            Assert.That(consumables.Refill(Consumable.Heart, 3), Is.Zero, "amount granted");
        }

        [Test]
        public void A_run_that_ran_out_of_hearts_cannot_continue_while_nothing_supplies_them()
        {
            var session = new GameSession(ClassicPuzzle(), OneHeart());
            session.Place(EmptyCell, WrongDigit);

            Assert.That(session.Status, Is.EqualTo(SessionStatus.Failed), "the run ended");
            Assert.That(session.ContinueWithMoreHearts(), Is.False, "nothing is selling hearts");
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Failed), "so it stays ended");
        }

        [Test]
        public void A_service_that_supplies_hearts_puts_the_player_back_in_the_run()
        {
            var consumables = new RecordingConsumables();
            var session = SessionWith(consumables, OneHeart());
            session.Place(EmptyCell, WrongDigit);

            // What a rewarded ad or a bundle will do, without gameplay changing.
            consumables.Supplies = true;

            Assert.That(session.ContinueWithMoreHearts(), Is.True, "the refill was supplied");
            Assert.That(session.Status, Is.EqualTo(SessionStatus.InProgress), "status");
            Assert.That(session.HeartsRemaining, Is.EqualTo(1), "hearts");
        }

        [Test]
        public void A_run_that_is_still_alive_cannot_be_continued()
        {
            var consumables = new RecordingConsumables { Supplies = true };
            var session = SessionWith(consumables);

            Assert.That(session.ContinueWithMoreHearts(), Is.False);
            Assert.That(consumables.RefillsAsked, Is.Zero,
                "a live run must not be able to buy its way to more hearts");
        }

        static RulesConfig OneHeart()
        {
            var rules = RulesConfig.Default;
            rules.Hearts = 1;
            return rules;
        }
    }
}
