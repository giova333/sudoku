namespace Sudoku.Core.Session
{
    /// <summary>
    /// One thing that happened during play, carrying the session's counters at
    /// that moment. A struct because CellPlaced fires roughly fifty times per
    /// puzzle and this stream should cost nothing to listen to.
    /// </summary>
    public readonly struct GameEvent
    {
        public GameEvent(GameEventKind kind, int cellIndex, int digit, bool wasCorrect,
            int heartsRemaining, int hintsRemaining, int mistakeCount, int hintsUsed,
            float elapsedSeconds, int emptyCellCount)
        {
            Kind = kind;
            CellIndex = cellIndex;
            Digit = digit;
            WasCorrect = wasCorrect;
            HeartsRemaining = heartsRemaining;
            HintsRemaining = hintsRemaining;
            MistakeCount = mistakeCount;
            HintsUsed = hintsUsed;
            ElapsedSeconds = elapsedSeconds;
            EmptyCellCount = emptyCellCount;
        }

        public GameEventKind Kind { get; }
        public int CellIndex { get; }
        public int Digit { get; }
        public bool WasCorrect { get; }
        public int HeartsRemaining { get; }
        public int HintsRemaining { get; }
        public int MistakeCount { get; }
        public int HintsUsed { get; }
        public float ElapsedSeconds { get; }
        public int EmptyCellCount { get; }
    }
}
