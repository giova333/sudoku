using System.Globalization;

namespace Sudoku.Core.Analytics
{
    /// <summary>
    /// One key and value on an event.
    ///
    /// Numbers are kept as numbers rather than formatted into text, because
    /// every SDK worth binding aggregates a numeric parameter and none of them
    /// aggregates a string. An adapter branches once on <see cref="IsNumber"/>
    /// and is done.
    /// </summary>
    public readonly struct AnalyticsParameter
    {
        AnalyticsParameter(string key, string text, double number, bool isNumber)
        {
            Key = key;
            Text = text;
            Number = number;
            IsNumber = isNumber;
        }

        public string Key { get; }

        /// <summary>The value of a text parameter; null when <see cref="IsNumber"/>.</summary>
        public string Text { get; }

        /// <summary>The value of a numeric parameter; zero when it is text.</summary>
        public double Number { get; }

        public bool IsNumber { get; }

        public static AnalyticsParameter Of(string key, string value) =>
            new AnalyticsParameter(key, value ?? string.Empty, 0d, false);

        public static AnalyticsParameter Of(string key, int value) =>
            new AnalyticsParameter(key, null, value, true);

        public static AnalyticsParameter Of(string key, float value) =>
            new AnalyticsParameter(key, null, value, true);

        /// <summary>
        /// Rendered for the console implementation and for test failures.
        /// Invariant culture, so a device set to a comma decimal separator
        /// cannot change what a recorded value looks like.
        /// </summary>
        public override string ToString() =>
            Key + "=" + (IsNumber ? Number.ToString("0.###", CultureInfo.InvariantCulture) : Text);
    }
}
