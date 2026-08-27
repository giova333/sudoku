using Sudoku.Core.Analytics;
using UnityEngine;

namespace Sudoku.Game.Analytics
{
    /// <summary>
    /// Writes every event to the player log and nowhere else.
    ///
    /// No SDK is bound in this milestone, deliberately: the point of shipping
    /// the schema first is that it can be read, argued with and corrected while
    /// changing it is free. Binding a real backend later means writing a second
    /// <see cref="IAnalyticsService"/> and passing it to
    /// <see cref="GameAnalytics"/> instead of this one - no event, parameter or
    /// call site moves.
    /// </summary>
    public sealed class ConsoleAnalyticsService : IAnalyticsService
    {
        public void Track(AnalyticsEvent recorded) => Debug.Log("[analytics] " + recorded);

        /// <summary>Nothing is buffered here - the log line is already written by
        /// the time <see cref="Track"/> returns.</summary>
        public void Flush() { }
    }
}
