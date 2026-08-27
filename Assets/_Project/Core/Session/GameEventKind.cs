namespace Sudoku.Core.Session
{
    /// <summary>
    /// Everything gameplay announces. Meta systems and analytics subscribe to
    /// this stream; gameplay never learns that they exist.
    /// </summary>
    public enum GameEventKind
    {
        PuzzleStarted,
        CellPlaced,
        MistakeMade,
        NoteToggled,
        UndoUsed,
        HintUsed,
        HeartsDepleted,
        PuzzleCompleted,
        PuzzleAbandoned
    }
}
