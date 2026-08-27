using System.Collections.Generic;

namespace Sudoku.Core.Analytics
{
    /// <summary>
    /// The parameters every event carries, held in one place and written onto
    /// each event by <see cref="AnalyticsReporter"/>.
    ///
    /// This exists so no call site ever has to remember them. A screen that
    /// reports a view knows nothing about which puzzle is in progress, and a
    /// placement deep inside a session knows nothing about the theme - but a
    /// funnel is worthless unless every event can be sliced by both. Attaching
    /// them centrally is also the only way a parameter added later reaches the
    /// events that were already being emitted.
    ///
    /// The properties are settable because these change while the app runs:
    /// the difficulty and the puzzle at every deal, the theme whenever the
    /// player switches it.
    /// </summary>
    public sealed class AnalyticsContext
    {
        /// <summary>Identifies this run of the app, so one player's events group
        /// into the sitting they happened in.</summary>
        public string SessionId { get; set; } = string.Empty;

        public string AppVersion { get; set; } = string.Empty;

        public string Platform { get; set; } = string.Empty;

        /// <summary>The theme in force. Set from the theme preference (#8) by the
        /// composition root, which is the only place that knows preferences exist.</summary>
        public string Theme { get; set; } = string.Empty;

        /// <summary>The tier being played, or empty outside a puzzle. The single
        /// most important slice there is - abandonment per difficulty is what
        /// says a tier is miscalibrated.</summary>
        public string Difficulty { get; set; } = string.Empty;

        /// <summary>Which puzzle, as its bank and index, so a tier's outliers can be
        /// found rather than guessed at.</summary>
        public string Puzzle { get; set; } = string.Empty;

        /// <summary>
        /// Appends every parameter that has a value. Empty ones are dropped
        /// rather than sent blank, so an event emitted outside a puzzle does not
        /// pollute a difficulty breakdown with a bucket of empty strings.
        /// </summary>
        public void WriteTo(List<AnalyticsParameter> into)
        {
            Append(into, "session_id", SessionId);
            Append(into, "app_version", AppVersion);
            Append(into, "platform", Platform);
            Append(into, "theme", Theme);
            Append(into, "difficulty", Difficulty);
            Append(into, "puzzle", Puzzle);
        }

        static void Append(List<AnalyticsParameter> into, string key, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            into.Add(AnalyticsParameter.Of(key, value));
        }
    }
}
