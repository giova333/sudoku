namespace Sudoku.Core.Persistence
{
    /// <summary>
    /// Brings an older payload up to the current schema before anything tries
    /// to read it. Every payload passes through here, including one already at
    /// the current version, so that the upgrade path is exercised on every
    /// single load rather than only on the launch after an update.
    /// </summary>
    static class SaveMigrations
    {
        internal static JsonValue Migrate(JsonValue root, int fromVersion)
        {
            if (fromVersion < 2)
                ToVersion2(root);
            if (fromVersion < 3)
                ToVersion3(root);

            root.Set("schemaVersion", JsonValue.Number(SaveSerializer.CurrentSchemaVersion));
            return root;
        }

        /// <summary>
        /// Version 1 held a single in-progress puzzle, because the greybox only
        /// ever had one, and left played-puzzle counts in engine preferences.
        /// Version 2 gives every difficulty its own slot and takes those counts
        /// into the save file, so the one slot becomes a list of one.
        /// </summary>
        static void ToVersion2(JsonValue root)
        {
            var slots = JsonValue.Array();
            var single = root.Member("slot");
            if (single != null && single.Kind == JsonKind.Object)
                slots.Add(single);

            root.Remove("slot");
            root.Set("slots", slots);

            if (root.Member("progress") == null)
                root.Set("progress", JsonValue.Array());
        }

        /// <summary>
        /// Version 3 records the best time per difficulty. Nothing was tracked
        /// before it, so an older save arrives with no records rather than with
        /// invented ones - the first solve after the update sets the first
        /// record, which is the honest outcome.
        /// </summary>
        static void ToVersion3(JsonValue root)
        {
            if (root.Member("best") == null)
                root.Set("best", JsonValue.Array());
        }
    }
}
