using System;
using System.Text;
using Sudoku.Core.Analytics;
using Sudoku.Game.Screens;
using Sudoku.Game.Settings;
using UnityEngine;

namespace Sudoku.Game.Analytics
{
    /// <summary>
    /// Plugs the reporter into the streams the game already publishes.
    ///
    /// Every subscription here is to something that existed before analytics
    /// did - the session's event stream, the navigator's screen changes, the
    /// one settings change stream - so nothing in gameplay, no screen and no
    /// preference has a line of analytics in it. A screen or a preference added
    /// tomorrow is reported without being wired up.
    ///
    /// This is also where the engine gets involved at all: the app version, the
    /// platform and the run's identity are the only things analytics needs that
    /// Sudoku.Core cannot know.
    /// </summary>
    public sealed class GameAnalytics
    {
        /// <summary>The key the theme preference declares itself under. Named rather
        /// than referenced so a preference rename cannot silently rename a reported
        /// parameter the funnels are already sliced by.</summary>
        const string ThemePreferenceKey = "theme";

        readonly AnalyticsReporter _reporter;

        public GameAnalytics(IAnalyticsService service)
        {
            _reporter = new AnalyticsReporter(service);

            _reporter.Context.SessionId = Guid.NewGuid().ToString("N");
            _reporter.Context.AppVersion = Application.version;
            _reporter.Context.Platform = Application.platform.ToString();

            // A value before anything has been observed, so an event emitted
            // between here and Observe(settings) is not reported themeless.
            _reporter.Context.Theme = "light";
        }

        /// <summary>Reports each screen the player lands on, however they got there.</summary>
        public void Observe(Navigator navigator)
        {
            if (navigator == null) throw new ArgumentNullException(nameof(navigator));

            navigator.Navigated += screen => _reporter.ScreenViewed(NameOf(screen));
        }

        /// <summary>
        /// Reports every preference change, and keeps the theme parameter that
        /// rides on every other event up to date.
        /// </summary>
        public void Observe(GameSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            foreach (var preference in settings.All)
                AdoptTheme(preference);

            settings.Changed += preference =>
            {
                AdoptTheme(preference);
                _reporter.SettingChanged(preference.Key, preference.ValueText);
            };
        }

        /// <summary>
        /// Follows the game screen from one puzzle to the next, stamping which
        /// puzzle is in play onto everything reported while it lasts. The
        /// presenter announces the session and knows nothing about who takes it.
        /// </summary>
        public void Observe(GamePresenter game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));

            game.SessionStarted += session =>
            {
                // Handed over before the parameters move, so anything the last
                // puzzle still had in hand is reported as that puzzle's.
                _reporter.Observe(session);

                _reporter.Context.Difficulty = game.Tier.ToString();
                _reporter.Context.Puzzle = game.PuzzleId;
            };
        }

        /// <summary>Empties the batch and the backend. For backgrounding and quitting,
        /// where there may be no later.</summary>
        public void Flush() => _reporter.Flush();

        void AdoptTheme(IPreference preference)
        {
            // Lower-cased because the schema's parameter values are, and the
            // preference persists the enum member's own casing.
            if (preference.Key == ThemePreferenceKey)
                _reporter.Context.Theme = preference.ValueText.ToLowerInvariant();
        }

        /// <summary>
        /// A screen's own type name, turned into the schema's snake_case:
        /// DifficultySelectView becomes difficulty_select. Derived rather than
        /// declared, because a table of screen names is a table someone forgets
        /// to add the new screen to.
        /// </summary>
        static string NameOf(IScreen screen)
        {
            var name = screen.GetType().Name;

            if (name.EndsWith("View", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "View".Length);
            else if (name.EndsWith("Presenter", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "Presenter".Length);

            var text = new StringBuilder(name.Length + 4);
            for (var i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                    text.Append('_');
                text.Append(char.ToLowerInvariant(name[i]));
            }

            return text.ToString();
        }
    }
}
