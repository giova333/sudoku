namespace Sudoku.Core.Commands
{
    /// <summary>
    /// One reversible change to a single cell: what its value and notes were
    /// before, and what they became.
    /// </summary>
    public readonly struct BoardEdit
    {
        public readonly int Index;
        public readonly int ValueBefore;
        public readonly int ValueAfter;
        public readonly int NotesBefore;
        public readonly int NotesAfter;

        public BoardEdit(int index, int valueBefore, int valueAfter, int notesBefore, int notesAfter)
        {
            Index = index;
            ValueBefore = valueBefore;
            ValueAfter = valueAfter;
            NotesBefore = notesBefore;
            NotesAfter = notesAfter;
        }
    }
}
