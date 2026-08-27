using System;

namespace Sudoku.Game.Settings
{
    /// <summary>
    /// A preference seen without knowing what it holds.
    ///
    /// Anything that cares about preferences in general rather than about one in
    /// particular - a "setting changed" analytics event, a diagnostics dump -
    /// works through this face, so declaring a new preference never edits it or
    /// its listeners.
    /// </summary>
    public interface IPreference
    {
        /// <summary>
        /// The stable storage key, and the name a change is reported under.
        /// Renaming one is a migration rather than a rename.
        /// </summary>
        string Key { get; }

        /// <summary>The current value as text, for a log line or an event payload.</summary>
        string ValueText { get; }

        /// <summary>
        /// Raised after the new value has been stored, so a listener that reads
        /// the preference back always sees the value the change announced.
        /// </summary>
        event Action<IPreference> Changed;
    }
}
