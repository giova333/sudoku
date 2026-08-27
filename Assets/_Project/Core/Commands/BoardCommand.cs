using System.Collections.Generic;

namespace Sudoku.Core.Commands
{
    /// <summary>
    /// One player action, however many cells it touched. Placing a digit that
    /// strikes it from twelve peer cells' notes is a single command, so a single
    /// undo puts all thirteen cells back.
    /// </summary>
    public sealed class BoardCommand
    {
        readonly List<BoardEdit> _edits;

        public BoardCommand(BoardCommandKind kind, int primaryIndex, List<BoardEdit> edits)
        {
            Kind = kind;
            PrimaryIndex = primaryIndex;
            _edits = edits;
        }

        public BoardCommandKind Kind { get; }

        /// <summary>The cell the player actually acted on.</summary>
        public int PrimaryIndex { get; }

        public IReadOnlyList<BoardEdit> Edits => _edits;

        public void Apply(int[] values, int[] notes)
        {
            for (var i = 0; i < _edits.Count; i++)
            {
                var e = _edits[i];
                values[e.Index] = e.ValueAfter;
                notes[e.Index] = e.NotesAfter;
            }
        }

        public void Revert(int[] values, int[] notes)
        {
            for (var i = _edits.Count - 1; i >= 0; i--)
            {
                var e = _edits[i];
                values[e.Index] = e.ValueBefore;
                notes[e.Index] = e.NotesBefore;
            }
        }
    }
}
