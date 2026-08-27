using System;
using NUnit.Framework;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Generation;
using Sudoku.Core.Model;
using Sudoku.Core.Persistence;
using Sudoku.Core.Session;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Persistence
{
    [TestFixture]
    public class SaveSerializerTests
    {
        static Puzzle ClassicPuzzle() =>
            Puzzle.FromStrings(KnownPuzzles.ClassicClues, KnownPuzzles.ClassicSolution);

        /// <summary>
        /// Deep enough hearts that an arbitrary run of random digits never ends
        /// the puzzle early, but the limit stays on so hearts still move.
        /// </summary>
        static RulesConfig ArbitraryRules()
        {
            var rules = RulesConfig.Default;
            rules.Hearts = 999;
            rules.Hints = 5;
            return rules;
        }

        /// <summary>
        /// An arbitrary but reproducible run of player actions: placements,
        /// pencil marks, erasures and undos, interleaved with the clock.
        /// </summary>
        static GameSession PlayArbitrarily(Puzzle puzzle, RulesConfig rules, int seed, int actions)
        {
            var session = new GameSession(puzzle, rules);
            session.Start();

            var random = new DeterministicRandom(seed);
            for (var i = 0; i < actions; i++)
            {
                var cell = random.Next(Board.CellCount);
                var digit = 1 + random.Next(Board.Size);

                switch (random.Next(6))
                {
                    case 0:
                    case 1:
                        session.Place(cell, digit);
                        break;
                    case 2:
                    case 3:
                        session.ToggleNote(cell, digit);
                        break;
                    case 4:
                        session.Erase(cell);
                        break;
                    default:
                        session.Undo();
                        break;
                }

                session.Tick(0.25f);
            }

            session.UseHint();
            session.Tick(1.75f);
            return session;
        }

        static SaveData SaveOf(GameSession session, Puzzle puzzle, RulesConfig rules, DifficultyTier tier)
        {
            var slot = SaveSlot.ForTier(tier, BankNameFor(tier), 137, puzzle, rules);
            slot.Session = session.Capture();
            slot.SavedAt = 1_700_000_000L;

            var data = new SaveData();
            data.PutSlot(slot);
            return data;
        }

        // The game layer builds this name; Core only records what it is told.
        static string BankNameFor(DifficultyTier tier) => "main-" + tier;

        static SaveData RoundTrip(SaveData data) => SaveSerializer.Read(SaveSerializer.Write(data));

        static GameSession RoundTripped(GameSession session, Puzzle puzzle, RulesConfig rules)
        {
            var reloaded = RoundTrip(SaveOf(session, puzzle, rules, DifficultyTier.Hard));
            return reloaded.SlotFor(DifficultyTier.Hard).ToSession();
        }

        [Test]
        public void An_arbitrary_run_of_moves_comes_back_with_the_same_board()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();
            var played = PlayArbitrarily(puzzle, rules, seed: 8117, actions: 140);

            var restored = RoundTripped(played, puzzle, rules);

            for (var i = 0; i < Board.CellCount; i++)
                Assert.That(restored.ValueAt(i), Is.EqualTo(played.ValueAt(i)), $"cell {i}");
        }

        [Test]
        public void An_arbitrary_run_of_moves_comes_back_with_the_same_pencil_marks()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();
            var played = PlayArbitrarily(puzzle, rules, seed: 8117, actions: 140);

            var restored = RoundTripped(played, puzzle, rules);

            for (var i = 0; i < Board.CellCount; i++)
                Assert.That(restored.NotesAt(i), Is.EqualTo(played.NotesAt(i)), $"notes at cell {i}");
        }

        [Test]
        public void An_arbitrary_run_of_moves_comes_back_with_the_same_clock_and_counters()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();
            var played = PlayArbitrarily(puzzle, rules, seed: 8117, actions: 140);

            var restored = RoundTripped(played, puzzle, rules);

            Assert.That(restored.ElapsedSeconds, Is.EqualTo(played.ElapsedSeconds), "elapsed");
            Assert.That(restored.HeartsRemaining, Is.EqualTo(played.HeartsRemaining), "hearts");
            Assert.That(restored.HintsRemaining, Is.EqualTo(played.HintsRemaining), "hints");
            Assert.That(restored.HintsUsed, Is.EqualTo(played.HintsUsed), "hints used");
            Assert.That(restored.MistakeCount, Is.EqualTo(played.MistakeCount), "mistakes");
            Assert.That(restored.EmptyCellCount, Is.EqualTo(played.EmptyCellCount), "empty cells");
            Assert.That(restored.Status, Is.EqualTo(played.Status), "status");
        }

        [Test]
        public void A_restored_undo_stack_unwinds_move_for_move_like_the_original()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();
            var played = PlayArbitrarily(puzzle, rules, seed: 4242, actions: 160);

            var restored = RoundTripped(played, puzzle, rules);
            Assert.That(restored.UndoDepth, Is.EqualTo(played.UndoDepth), "undo depth");

            while (played.UndoDepth > 0)
            {
                played.Undo();
                Assert.That(restored.Undo(), Is.True, "the restored stack ran out first");

                for (var i = 0; i < Board.CellCount; i++)
                    Assert.That(restored.ValueAt(i), Is.EqualTo(played.ValueAt(i)),
                        $"cell {i} after unwinding to depth {played.UndoDepth}");
            }

            Assert.That(restored.UndoDepth, Is.Zero, "the restored stack should be exhausted too");
        }

        [Test]
        public void The_undo_stack_is_persisted_capped_at_the_history_limit()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();
            var played = PlayArbitrarily(puzzle, rules, seed: 61, actions: 900);

            var restored = RoundTripped(played, puzzle, rules);

            Assert.That(restored.UndoDepth, Is.EqualTo(GameSession.PersistedHistoryLimit));
        }

        [Test]
        public void The_rules_a_puzzle_was_started_under_survive_a_round_trip()
        {
            var puzzle = ClassicPuzzle();
            var rules = RulesConfig.Default;
            rules.Hearts = 7;
            rules.Hints = 2;
            rules.MistakeLimitEnabled = false;
            rules.AutoRemoveNotes = false;

            var slot = RoundTrip(SaveOf(new GameSession(puzzle, rules), puzzle, rules, DifficultyTier.Master))
                .SlotFor(DifficultyTier.Master);

            Assert.That(slot.Rules.Hearts, Is.EqualTo(7), "hearts");
            Assert.That(slot.Rules.Hints, Is.EqualTo(2), "hints");
            Assert.That(slot.Rules.MistakeLimitEnabled, Is.False, "mistake limit");
            Assert.That(slot.Rules.AutoRemoveNotes, Is.False, "auto-remove notes");
        }

        [Test]
        public void A_slot_remembers_which_bank_puzzle_the_player_is_on()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();

            var slot = RoundTrip(SaveOf(new GameSession(puzzle, rules), puzzle, rules, DifficultyTier.Expert))
                .SlotFor(DifficultyTier.Expert);

            Assert.That(slot.BankName, Is.EqualTo("main-Expert"), "bank");
            Assert.That(slot.BankIndex, Is.EqualTo(137), "index");
        }

        [Test]
        public void Every_difficulty_keeps_a_slot_of_its_own()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();
            var data = new SaveData();

            foreach (DifficultyTier tier in Enum.GetValues(typeof(DifficultyTier)))
                data.PutSlot(SaveSlot.ForTier(tier, "main-" + tier, (int)tier, puzzle, rules));

            var reloaded = RoundTrip(data);

            foreach (DifficultyTier tier in Enum.GetValues(typeof(DifficultyTier)))
                Assert.That(reloaded.SlotFor(tier).BankIndex, Is.EqualTo((int)tier), $"{tier} slot");
        }

        [Test]
        public void Starting_an_easy_game_leaves_a_half_finished_expert_alone()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();
            var expert = PlayArbitrarily(puzzle, rules, seed: 909, actions: 40);

            var data = new SaveData();
            var expertSlot = SaveSlot.ForTier(DifficultyTier.Expert, "main-Expert", 4, puzzle, rules);
            expertSlot.Session = expert.Capture();
            data.PutSlot(expertSlot);
            data.PutSlot(SaveSlot.ForTier(DifficultyTier.Easy, "main-Easy", 11, puzzle, rules));

            var reloaded = RoundTrip(data).SlotFor(DifficultyTier.Expert).ToSession();

            Assert.That(reloaded.UndoDepth, Is.EqualTo(expert.UndoDepth));
        }

        [Test]
        public void The_daily_puzzle_is_kept_in_a_slot_of_its_own()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();
            var data = new SaveData();
            data.PutSlot(SaveSlot.ForTier(DifficultyTier.Medium, "main-Medium", 3, puzzle, rules));
            data.PutSlot(SaveSlot.ForDaily(new DateTime(2026, 8, 27), DifficultyTier.Medium,
                "daily-Medium", 88, puzzle, rules));

            var reloaded = RoundTrip(data);

            Assert.That(reloaded.DailySlot.DateKey, Is.EqualTo("2026-08-27"));
        }

        [Test]
        public void Clearing_one_slot_leaves_the_others_untouched()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();
            var data = new SaveData();
            data.PutSlot(SaveSlot.ForTier(DifficultyTier.Easy, "main-Easy", 1, puzzle, rules));
            data.PutSlot(SaveSlot.ForTier(DifficultyTier.Hard, "main-Hard", 2, puzzle, rules));

            data.ClearSlot(SaveSlot.IdFor(DifficultyTier.Easy));
            var reloaded = RoundTrip(data);

            Assert.That(reloaded.SlotFor(DifficultyTier.Easy), Is.Null, "the cleared slot");
            Assert.That(reloaded.SlotFor(DifficultyTier.Hard), Is.Not.Null, "the untouched slot");
        }

        [Test]
        public void Played_puzzle_tracking_survives_a_round_trip()
        {
            var data = new SaveData();
            var easy = data.ProgressFor(DifficultyTier.Easy);
            easy.Played = 37;
            easy.Offset = 811;

            var reloaded = RoundTrip(data).ProgressFor(DifficultyTier.Easy);

            Assert.That(reloaded.Played, Is.EqualTo(37), "played");
            Assert.That(reloaded.Offset, Is.EqualTo(811), "offset");
        }

        [Test]
        public void An_untouched_tier_has_no_played_puzzle_history_to_start_with()
        {
            var progress = new SaveData().ProgressFor(DifficultyTier.Master);

            Assert.That(progress.Played, Is.Zero);
        }

        [Test]
        public void The_puzzle_offered_to_continue_is_the_most_recently_saved_one()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();
            var data = new SaveData();

            var older = SaveSlot.ForTier(DifficultyTier.Easy, "main-Easy", 1, puzzle, rules);
            older.SavedAt = 1_000;
            var newer = SaveSlot.ForTier(DifficultyTier.Expert, "main-Expert", 2, puzzle, rules);
            newer.SavedAt = 2_000;
            data.PutSlot(older);
            data.PutSlot(newer);

            Assert.That(RoundTrip(data).MostRecent().Tier, Is.EqualTo(DifficultyTier.Expert));
        }

        [Test]
        public void A_finished_puzzle_is_not_offered_to_continue()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();
            var data = new SaveData();

            var slot = SaveSlot.ForTier(DifficultyTier.Easy, "main-Easy", 1, puzzle, rules);
            slot.Session.Status = SessionStatus.Completed;
            data.PutSlot(slot);

            Assert.That(RoundTrip(data).MostRecent(), Is.Null);
        }

        [Test]
        public void Every_payload_declares_the_current_schema_version()
        {
            var json = SaveSerializer.Write(new SaveData());

            Assert.That(json.Contains($"\"schemaVersion\":{SaveSerializer.CurrentSchemaVersion}"), Is.True,
                $"the payload should declare its schema version: {json}");
        }

        [Test]
        public void A_save_from_a_future_build_is_rejected_rather_than_misread()
        {
            var json = SaveSerializer.Write(new SaveData())
                .Replace($"\"schemaVersion\":{SaveSerializer.CurrentSchemaVersion}", "\"schemaVersion\":99");

            Assert.Throws<SaveFormatException>(() => SaveSerializer.Read(json));
        }

        [Test]
        public void A_payload_that_is_not_json_is_rejected_rather_than_half_read()
        {
            Assert.Throws<SaveFormatException>(() => SaveSerializer.Read("{\"schemaVersion\": 2, \"slots\": ["));
        }

        [Test]
        public void A_slot_whose_board_has_been_truncated_is_rejected()
        {
            var puzzle = ClassicPuzzle();
            var rules = ArbitraryRules();
            var json = SaveSerializer.Write(SaveOf(new GameSession(puzzle, rules), puzzle, rules,
                DifficultyTier.Easy)).Replace(KnownPuzzles.ClassicClues, "530070000");

            Assert.Throws<SaveFormatException>(() => SaveSerializer.Read(json));
        }
    }
}
