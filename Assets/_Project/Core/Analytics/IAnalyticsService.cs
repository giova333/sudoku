namespace Sudoku.Core.Analytics
{
    /// <summary>
    /// The one seam an analytics SDK is bound at.
    ///
    /// Everything above it - the event names, the common parameters, the
    /// batching of cell placements - belongs to <see cref="AnalyticsReporter"/>
    /// and is shared by every backend. Adopting a real SDK is therefore writing
    /// these two methods and changing the one line in the composition root that
    /// picks the implementation; nothing else in the game names a backend.
    ///
    /// It lives in Sudoku.Core because an event schema is not an engine
    /// concern. The console implementation is in Sudoku.Game, where the app
    /// version and the platform can be read.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Records one event, already carrying its common parameters. Called on
        /// the main thread from inside a player's move, so an implementation
        /// that talks to a network must buffer rather than block.
        /// </summary>
        void Track(AnalyticsEvent recorded);

        /// <summary>
        /// Hands anything buffered to the backend. Called when the app is
        /// backgrounded, where there may be no later.
        /// </summary>
        void Flush();
    }
}
