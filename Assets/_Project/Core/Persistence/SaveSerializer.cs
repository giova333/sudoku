using System.Collections.Generic;
using System.Text;
using Sudoku.Core.Commands;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using Sudoku.Core.Session;

namespace Sudoku.Core.Persistence
{
    /// <summary>
    /// Turns everything the game remembers into one JSON payload and back.
    ///
    /// This is the round-trip seam: a session driven through an arbitrary run
    /// of moves, written, read back and asserted equivalent. Keeping it in Core
    /// rather than behind the engine's serializer is what lets that test run
    /// with no editor, no scene and no file system.
    ///
    /// The shape is array-based on purpose - grids as 81-character strings, the
    /// undo stack as one flat run of ints - so the payload stays small enough
    /// to write after every move on a phone.
    /// </summary>
    public static class SaveSerializer
    {
        /// <summary>
        /// Bumped whenever the payload's shape changes. Version 1 was the
        /// greybox single-slot format; version 2 gives every difficulty a slot
        /// and absorbs played-puzzle tracking.
        /// </summary>
        public const int CurrentSchemaVersion = 2;

        // Five ints per edit: cell, value before and after, notes before and after.
        const int IntsPerEdit = 5;

        public static string Write(SaveData data)
        {
            var root = JsonValue.Object();
            root.Set("schemaVersion", JsonValue.Number(CurrentSchemaVersion));

            var slots = JsonValue.Array();
            foreach (var slot in data.Slots)
                slots.Add(WriteSlot(slot));
            root.Set("slots", slots);

            var progress = JsonValue.Array();
            foreach (var entry in data.Progress)
            {
                var item = JsonValue.Object();
                item.Set("tier", JsonValue.Number((int)entry.Tier));
                item.Set("played", JsonValue.Number(entry.Played));
                item.Set("offset", JsonValue.Number(entry.Offset));
                progress.Add(item);
            }
            root.Set("progress", progress);

            var sb = new StringBuilder(2048);
            root.Write(sb);
            return sb.ToString();
        }

        public static SaveData Read(string json)
        {
            var root = JsonValue.Parse(json);
            if (root.Kind != JsonKind.Object)
                throw new SaveFormatException("A save payload must be a JSON object.");

            var version = root.IntOr("schemaVersion", 0);
            if (version <= 0)
                throw new SaveFormatException("A save payload must declare a positive schemaVersion.");
            if (version > CurrentSchemaVersion)
                throw new SaveFormatException(
                    $"This save was written by a newer build (schema {version}; this build reads " +
                    $"{CurrentSchemaVersion}).");

            root = SaveMigrations.Migrate(root, version);

            var data = new SaveData { SchemaVersion = CurrentSchemaVersion };

            var slots = root.Member("slots");
            if (slots != null)
                foreach (var item in slots.Items)
                    data.PutSlot(ReadSlot(item));

            var progress = root.Member("progress");
            if (progress != null)
                foreach (var item in progress.Items)
                {
                    var entry = data.ProgressFor((DifficultyTier)item.IntOr("tier", 0));
                    entry.Played = item.IntOr("played", 0);
                    entry.Offset = item.IntOr("offset", -1);
                }

            return data;
        }

        static JsonValue WriteSlot(SaveSlot slot)
        {
            var item = JsonValue.Object();
            item.Set("id", JsonValue.Text(slot.SlotId));
            item.Set("tier", JsonValue.Number((int)slot.Tier));
            item.Set("date", JsonValue.Text(slot.DateKey ?? string.Empty));
            item.Set("bank", JsonValue.Text(slot.BankName ?? string.Empty));
            item.Set("bankIndex", JsonValue.Number(slot.BankIndex));
            item.Set("clues", JsonValue.Text(slot.Clues));
            item.Set("solution", JsonValue.Text(slot.Solution));

            var rules = slot.Rules ?? RulesConfig.Default;
            item.Set("rules", JsonValue.Numbers(new[]
            {
                rules.Hearts,
                rules.MistakeLimitEnabled ? 1 : 0,
                rules.Hints,
                rules.AutoRemoveNotes ? 1 : 0
            }));

            var session = slot.Session ?? new SessionSnapshot();
            item.Set("values", JsonValue.Text(GridParser.ToText(session.Values)));
            item.Set("notes", JsonValue.Numbers(session.Notes));
            item.Set("elapsed", JsonValue.Number(session.ElapsedSeconds));
            item.Set("hearts", JsonValue.Number(session.HeartsRemaining));
            item.Set("hints", JsonValue.Number(session.HintsRemaining));
            item.Set("hintsUsed", JsonValue.Number(session.HintsUsed));
            item.Set("mistakes", JsonValue.Number(session.MistakeCount));
            item.Set("status", JsonValue.Number((int)session.Status));
            item.Set("paused", JsonValue.Bool(session.IsPaused));
            item.Set("started", JsonValue.Bool(session.Started));
            item.Set("history", WriteHistory(session.History));
            item.Set("savedAt", JsonValue.Number(slot.SavedAt));
            return item;
        }

        static SaveSlot ReadSlot(JsonValue item)
        {
            if (item == null || item.Kind != JsonKind.Object)
                throw new SaveFormatException("A save slot must be a JSON object.");

            var rules = item.IntsOr("rules", null);
            var snapshot = new SessionSnapshot
            {
                Values = GridParser.Parse(Grid(item.TextOr("values", null), "values")),
                Notes = Notes(item.IntsOr("notes", null)),
                ElapsedSeconds = item.FloatOr("elapsed", 0f),
                HeartsRemaining = item.IntOr("hearts", 0),
                HintsRemaining = item.IntOr("hints", 0),
                HintsUsed = item.IntOr("hintsUsed", 0),
                MistakeCount = item.IntOr("mistakes", 0),
                Status = (SessionStatus)item.IntOr("status", (int)SessionStatus.InProgress),
                IsPaused = item.BoolOr("paused", false),
                Started = item.BoolOr("started", false),
                History = ReadHistory(item.IntsOr("history", null))
            };

            return new SaveSlot
            {
                SlotId = item.TextOr("id", string.Empty),
                Tier = (DifficultyTier)item.IntOr("tier", 0),
                DateKey = item.TextOr("date", string.Empty),
                BankName = item.TextOr("bank", string.Empty),
                BankIndex = item.IntOr("bankIndex", 0),
                Clues = Grid(item.TextOr("clues", null), "clues"),
                Solution = Grid(item.TextOr("solution", null), "solution"),
                Rules = ReadRules(rules),
                Session = snapshot,
                SavedAt = item.LongOr("savedAt", 0L)
            };
        }

        static RulesConfig ReadRules(int[] values)
        {
            var rules = RulesConfig.Default;
            if (values == null || values.Length < 4)
                return rules;

            rules.Hearts = values[0];
            rules.MistakeLimitEnabled = values[1] != 0;
            rules.Hints = values[2];
            rules.AutoRemoveNotes = values[3] != 0;
            return rules;
        }

        static string Grid(string text, string what)
        {
            if (text == null || text.Length != Board.CellCount)
                throw new SaveFormatException(
                    $"A save slot's {what} must be {Board.CellCount} characters.");

            return text;
        }

        static int[] Notes(int[] values)
        {
            if (values == null || values.Length != Board.CellCount)
                throw new SaveFormatException(
                    $"A save slot's notes must hold {Board.CellCount} masks.");

            return values;
        }

        /// <summary>
        /// The undo stack as one flat run of ints: kind, cell, edit count, then
        /// five ints per edit. A stack of two hundred composite commands is a
        /// few thousand numbers, which is small enough to rewrite after every
        /// move.
        /// </summary>
        static JsonValue WriteHistory(List<BoardCommand> history)
        {
            var flat = new List<int>();
            if (history != null)
                foreach (var command in history)
                {
                    flat.Add((int)command.Kind);
                    flat.Add(command.PrimaryIndex);
                    flat.Add(command.Edits.Count);

                    foreach (var edit in command.Edits)
                    {
                        flat.Add(edit.Index);
                        flat.Add(edit.ValueBefore);
                        flat.Add(edit.ValueAfter);
                        flat.Add(edit.NotesBefore);
                        flat.Add(edit.NotesAfter);
                    }
                }

            return JsonValue.Numbers(flat);
        }

        static List<BoardCommand> ReadHistory(int[] flat)
        {
            var history = new List<BoardCommand>();
            if (flat == null)
                return history;

            var at = 0;
            while (at < flat.Length)
            {
                if (at + 3 > flat.Length)
                    throw new SaveFormatException("A saved undo stack ended mid-command.");

                var kind = flat[at++];
                var primary = flat[at++];
                var count = flat[at++];

                if (kind < 0 || kind > (int)BoardCommandKind.Hint)
                    throw new SaveFormatException($"A saved undo entry names an unknown command kind {kind}.");
                if (count < 0 || at + count * IntsPerEdit > flat.Length)
                    throw new SaveFormatException("A saved undo stack ended mid-command.");

                var edits = new List<BoardEdit>(count);
                for (var i = 0; i < count; i++)
                {
                    var index = flat[at];
                    if (index < 0 || index >= Board.CellCount)
                        throw new SaveFormatException($"A saved undo entry names cell {index}.");

                    edits.Add(new BoardEdit(index, flat[at + 1], flat[at + 2], flat[at + 3], flat[at + 4]));
                    at += IntsPerEdit;
                }

                history.Add(new BoardCommand((BoardCommandKind)kind, primary, edits));
            }

            return history;
        }
    }
}
