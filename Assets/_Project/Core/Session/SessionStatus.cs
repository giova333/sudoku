namespace Sudoku.Core.Session
{
    public enum SessionStatus
    {
        /// <summary>The player is still solving. The only state that accepts moves.</summary>
        InProgress,

        /// <summary>Every cell holds its solution digit.</summary>
        Completed,

        /// <summary>The mistake allowance ran out.</summary>
        Failed
    }
}
