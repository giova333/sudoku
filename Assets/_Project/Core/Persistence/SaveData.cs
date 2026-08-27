using System.Collections.Generic;
using Sudoku.Core.Difficulty;

namespace Sudoku.Core.Persistence
{
    /// <summary>
    /// The whole of what the game remembers between launches: one in-progress
    /// slot per difficulty, one for the daily, and how far through each bank the
    /// player has walked.
    ///
    /// Slots are keyed by id rather than indexed by tier so that starting a
    /// quick Easy game cannot reach a half-finished Expert, and so that adding a
    /// slot kind later is a new id rather than a new schema.
    /// </summary>
    public sealed class SaveData
    {
        readonly List<SaveSlot> _slots = new List<SaveSlot>();
        readonly List<BankProgress> _progress = new List<BankProgress>();

        /// <summary>The schema the payload was written against. See <see cref="SaveSerializer"/>.</summary>
        public int SchemaVersion { get; set; } = SaveSerializer.CurrentSchemaVersion;

        public IReadOnlyList<SaveSlot> Slots => _slots;

        public IReadOnlyList<BankProgress> Progress => _progress;

        public SaveSlot Slot(string slotId)
        {
            foreach (var slot in _slots)
                if (slot.SlotId == slotId)
                    return slot;

            return null;
        }

        /// <summary>The in-progress puzzle for one difficulty, or null when there is none.</summary>
        public SaveSlot SlotFor(DifficultyTier tier) => Slot(SaveSlot.IdFor(tier));

        /// <summary>The in-progress daily puzzle, whatever date it belongs to.</summary>
        public SaveSlot DailySlot => Slot(SaveSlot.DailySlotId);

        /// <summary>
        /// The puzzle waiting under one difficulty, or null when that tier has
        /// nothing to go back to. Distinct from <see cref="SlotFor"/>, which
        /// still answers with a finished or failed run: this is the question a
        /// difficulty picker asks when it marks the tiers with a game in
        /// progress.
        /// </summary>
        public SaveSlot ResumableFor(DifficultyTier tier)
        {
            var slot = SlotFor(tier);
            return slot != null && slot.CanResume ? slot : null;
        }

        /// <summary>Stores a slot, replacing whatever was under the same id.</summary>
        public void PutSlot(SaveSlot slot)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].SlotId != slot.SlotId) continue;
                _slots[i] = slot;
                return;
            }

            _slots.Add(slot);
        }

        /// <summary>Forgets one slot. Returns false when there was nothing there.</summary>
        public bool ClearSlot(string slotId)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].SlotId != slotId) continue;
                _slots.RemoveAt(i);
                return true;
            }

            return false;
        }

        /// <summary>
        /// The puzzle a Continue button should offer: the most recently written
        /// slot that is still playable, or null when the player has nothing
        /// waiting.
        /// </summary>
        public SaveSlot MostRecent()
        {
            SaveSlot best = null;
            foreach (var slot in _slots)
            {
                if (!slot.CanResume) continue;
                if (best == null || slot.SavedAt > best.SavedAt)
                    best = slot;
            }

            return best;
        }

        /// <summary>
        /// This tier's walk through its bank, created on first ask so callers
        /// never have to decide whether it already exists.
        /// </summary>
        public BankProgress ProgressFor(DifficultyTier tier)
        {
            foreach (var progress in _progress)
                if (progress.Tier == tier)
                    return progress;

            var created = new BankProgress(tier);
            _progress.Add(created);
            return created;
        }
    }
}
