using System;

namespace Sudoku.Core.Persistence
{
    /// <summary>
    /// Raised when a payload is not a save this build can read. A save that
    /// cannot be understood must say so rather than restore half a board, which
    /// would look to the player like the game had eaten their puzzle.
    /// </summary>
    public sealed class SaveFormatException : Exception
    {
        public SaveFormatException(string message) : base(message) { }
    }
}
