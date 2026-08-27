using System;
using System.Globalization;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using Sudoku.Core.Session;

namespace Sudoku.Core.Persistence
{
    /// <summary>
    /// One puzzle the player has in flight, and everything needed to put them
    /// back in front of it.
    ///
    /// The grid is stored inline next to the bank reference rather than instead
    /// of it: the reference is bookkeeping, the grid is the truth. A re-baked
    /// bank shifts every index, and a half-finished Expert must not turn into
    /// somebody else's puzzle because the content was recalibrated.
    /// </summary>
    public sealed class SaveSlot
    {
        /// <summary>
        /// The id every daily puzzle shares. There is only ever one daily slot;
        /// yesterday's is replaced rather than kept.
        /// </summary>
        public const string DailySlotId = "daily";

        /// <summary>Identifies the slot: the tier's name, or <see cref="DailySlotId"/>.</summary>
        public string SlotId { get; set; }

        public DifficultyTier Tier { get; set; }

        /// <summary>The ISO date a daily slot belongs to; empty for a tier slot.</summary>
        public string DateKey { get; set; } = string.Empty;

        /// <summary>The bank this puzzle was dealt from, for bookkeeping and analytics.</summary>
        public string BankName { get; set; } = string.Empty;

        public int BankIndex { get; set; }

        /// <summary>The puzzle's 81 clue characters.</summary>
        public string Clues { get; set; }

        /// <summary>The puzzle's 81 solution characters.</summary>
        public string Solution { get; set; }

        /// <summary>The rules the puzzle was started under, so settings changes cannot rewrite history.</summary>
        public RulesConfig Rules { get; set; }

        public SessionSnapshot Session { get; set; }

        /// <summary>
        /// Unix seconds at the last write. Used only to order slots for a
        /// Continue button - never to compute an elapsed time, which is
        /// accumulated from frame deltas and must stay immune to the clock.
        /// </summary>
        public long SavedAt { get; set; }

        public bool IsDaily => SlotId == DailySlotId;

        /// <summary>True when there is a puzzle here the player could go back to.</summary>
        public bool CanResume => Session != null && Session.Status == SessionStatus.InProgress;

        public static string IdFor(DifficultyTier tier) => tier.ToString();

        public static string DateKeyFor(DateTime date) =>
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>A fresh slot holding an untouched puzzle for one difficulty.</summary>
        public static SaveSlot ForTier(DifficultyTier tier, string bankName, int bankIndex,
            Puzzle puzzle, RulesConfig rules) =>
            New(IdFor(tier), tier, string.Empty, bankName, bankIndex, puzzle, rules);

        /// <summary>A fresh slot holding an untouched daily puzzle.</summary>
        public static SaveSlot ForDaily(DateTime date, DifficultyTier tier, string bankName, int bankIndex,
            Puzzle puzzle, RulesConfig rules) =>
            New(DailySlotId, tier, DateKeyFor(date), bankName, bankIndex, puzzle, rules);

        static SaveSlot New(string slotId, DifficultyTier tier, string dateKey, string bankName, int bankIndex,
            Puzzle puzzle, RulesConfig rules)
        {
            if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            var clues = new int[Board.CellCount];
            var solution = new int[Board.CellCount];
            for (var i = 0; i < Board.CellCount; i++)
            {
                clues[i] = puzzle.ClueAt(i);
                solution[i] = puzzle.SolutionAt(i);
            }

            return new SaveSlot
            {
                SlotId = slotId,
                Tier = tier,
                DateKey = dateKey,
                BankName = bankName ?? string.Empty,
                BankIndex = bankIndex,
                Clues = GridParser.ToText(clues),
                Solution = GridParser.ToText(solution),
                Rules = rules,
                Session = new GameSession(puzzle, rules).Capture()
            };
        }

        public Puzzle ToPuzzle() => Puzzle.FromStrings(Clues, Solution);

        /// <summary>
        /// The session the player left, ready to be handed straight back to the
        /// presenter. Restoring never replays moves, so it costs the same
        /// whether the player was three moves in or seventy.
        /// </summary>
        public GameSession ToSession()
        {
            var puzzle = ToPuzzle();
            var rules = Rules ?? RulesConfig.Default;

            // No snapshot means a slot that was written before anyone touched
            // the board: hand back a new session rather than an empty grid.
            return Session == null
                ? new GameSession(puzzle, rules)
                : GameSession.Restore(puzzle, rules, Session);
        }
    }
}
