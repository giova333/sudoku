namespace Sudoku.Game.Settings
{
    /// <summary>
    /// Where preferences live between launches.
    ///
    /// It trades only in text, so it never learns the type or the meaning of
    /// anything it holds - which is what lets a new preference be declared
    /// without touching storage at all.
    /// </summary>
    public interface IPreferenceStore
    {
        /// <summary>Reads a key, or hands back the fallback when it has never been written.</summary>
        string Read(string key, string fallback);

        void Write(string key, string value);

        /// <summary>
        /// Commits pending writes. Called on every change rather than at
        /// shutdown, because a phone can take the process away without warning.
        /// </summary>
        void Flush();
    }
}
