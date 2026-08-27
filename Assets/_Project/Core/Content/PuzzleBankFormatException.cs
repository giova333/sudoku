using System;

namespace Sudoku.Core.Content
{
    /// <summary>Raised when a bank's bytes are not a bank this build can read.</summary>
    public sealed class PuzzleBankFormatException : Exception
    {
        public PuzzleBankFormatException(string message) : base(message) { }
    }
}
