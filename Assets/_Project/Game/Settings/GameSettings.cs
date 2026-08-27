using System;
using System.Collections.Generic;
using Sudoku.Core.Session;

namespace Sudoku.Game.Settings
{
    /// <summary>
    /// The one place a preference is read from.
    ///
    /// No component keeps a preference of its own: views and presenters are
    /// handed this and ask it, so there is a single answer to "is the timer
    /// visible" and a single place a change is announced from. That is the whole
    /// point of the type - a second copy of a preference is a bug waiting for
    /// the settings screen to disagree with the game.
    ///
    /// Adding a preference is one declaration in the constructor plus one row on
    /// the settings screen. The store, the change stream and every existing
    /// listener are untouched by it - which is how the theme choice (#8) and the
    /// two mutes below arrive without a rewrite.
    /// </summary>
    public sealed class GameSettings
    {
        readonly List<IPreference> _all = new List<IPreference>();

        public GameSettings(IPreferenceStore store)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));

            MistakeLimit = Declare(new Preference<bool>(store, "mistakeLimit", true));
            HighlightMistakes = Declare(new Preference<bool>(store, "highlightMistakes", true));
            AutoRemoveNotes = Declare(new Preference<bool>(store, "autoRemoveNotes", true));
            TimerVisible = Declare(new Preference<bool>(store, "timerVisible", true));
            SoundEnabled = Declare(new Preference<bool>(store, "sound", true));
            HapticsEnabled = Declare(new Preference<bool>(store, "haptics", true));

            // The theme choice is declared here by ticket #8, next to the rest.
        }

        /// <summary>Whether a mistake costs a heart. Off is a relaxed, consequence-free game.</summary>
        public Preference<bool> MistakeLimit { get; }

        /// <summary>Whether a wrong digit is marked the moment it is placed, or left
        /// for the player to find.</summary>
        public Preference<bool> HighlightMistakes { get; }

        /// <summary>Whether placing a digit clears that digit from its peers' notes.</summary>
        public Preference<bool> AutoRemoveNotes { get; }

        /// <summary>Whether the clock is shown. It always runs - hiding it removes the
        /// pressure, not the record.</summary>
        public Preference<bool> TimerVisible { get; }

        /// <summary>Sound and haptics mute independently, because the reasons to silence
        /// one are not the reasons to silence the other. Ticket #11 gives them something
        /// to drive.</summary>
        public Preference<bool> SoundEnabled { get; }

        /// <summary><see cref="SoundEnabled"/>'s independent twin.</summary>
        public Preference<bool> HapticsEnabled { get; }

        /// <summary>
        /// Raised for every preference change, whichever one it was. Analytics
        /// (#13) subscribes once here rather than once per preference, so a new
        /// preference is reported without being wired up.
        /// </summary>
        public event Action<IPreference> Changed;

        /// <summary>Every declared preference, for anything that treats them as a set.</summary>
        public IReadOnlyList<IPreference> All => _all;

        /// <summary>
        /// The rules a new puzzle is dealt under.
        ///
        /// A snapshot, deliberately: taking hearts away from a puzzle already in
        /// progress - or handing them back - would rewrite a game that is
        /// already being scored, so a mistake-limit change waits for the next
        /// deal. The settings screen says as much.
        /// </summary>
        public RulesConfig BuildRules()
        {
            var rules = RulesConfig.Default;
            rules.MistakeLimitEnabled = MistakeLimit.Value;
            rules.AutoRemoveNotes = AutoRemoveNotes.Value;
            return rules;
        }

        Preference<T> Declare<T>(Preference<T> preference)
        {
            preference.Changed += OnPreferenceChanged;
            _all.Add(preference);
            return preference;
        }

        void OnPreferenceChanged(IPreference preference) => Changed?.Invoke(preference);
    }
}
