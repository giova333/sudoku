using UnityEngine;

namespace Sudoku.Game.Settings
{
    /// <summary>
    /// Preferences on top of <see cref="PlayerPrefs"/>.
    ///
    /// A handful of small values that must survive a reinstall-free relaunch is
    /// exactly what PlayerPrefs is for; the save file ticket #2 writes is for
    /// puzzle state, which is large, structured and versioned. Keeping the two
    /// apart means a corrupt save never costs the player their settings.
    /// </summary>
    public sealed class PlayerPrefsStore : IPreferenceStore
    {
        /// <summary>Namespaces the keys so they cannot collide with the play-tracking counters.</summary>
        const string Prefix = "settings.";

        public string Read(string key, string fallback) => PlayerPrefs.GetString(Prefix + key, fallback);

        public void Write(string key, string value) => PlayerPrefs.SetString(Prefix + key, value);

        public void Flush() => PlayerPrefs.Save();
    }
}
