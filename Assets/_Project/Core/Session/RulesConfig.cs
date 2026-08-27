namespace Sudoku.Core.Session
{
    /// <summary>
    /// The player-facing rule toggles for a session. Defaults match the
    /// shipping configuration; settings screens vary them.
    /// </summary>
    public sealed class RulesConfig
    {
        /// <summary>
        /// How many wrong placements the player may make. When
        /// <see cref="MistakeLimitEnabled"/> is false this is ignored.
        /// </summary>
        public int Hearts { get; set; } = 3;

        public bool MistakeLimitEnabled { get; set; } = true;

        public int Hints { get; set; } = 3;

        /// <summary>
        /// When a digit is placed, strike it from the notes of every peer cell
        /// so the player is not left doing bookkeeping by hand.
        /// </summary>
        public bool AutoRemoveNotes { get; set; } = true;

        public static RulesConfig Default => new RulesConfig();
    }
}
