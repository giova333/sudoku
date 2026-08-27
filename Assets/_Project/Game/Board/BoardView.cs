using System;
using Sudoku.Core.Model;
using Sudoku.Core.Session;
using Sudoku.Game.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Board
{
    /// <summary>
    /// The 9x9 grid. Cells are positioned explicitly rather than by a layout
    /// group so the thicker box separators fall in the right places.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        static readonly Color GridLine = new Color(0.35f, 0.37f, 0.42f);
        static readonly Color BoardBackground = new Color(0.35f, 0.37f, 0.42f);

        CellView[] _cells;

        /// <summary>Raised when the player taps a cell.</summary>
        public Action<int> CellTapped;

        public static BoardView Create(Transform parent, float boardSize)
        {
            var rect = Ui.Rect("Board", parent);
            var view = rect.gameObject.AddComponent<BoardView>();

            var background = rect.gameObject.AddComponent<Image>();
            background.color = BoardBackground;
            background.raycastTarget = false;

            const float thin = 1f;
            const float thick = 3f;
            const float outer = 3f;

            // Solve for a cell size that leaves room for the separators.
            var separators = outer * 2 + thick * 2 + thin * 6;
            var cellSize = (boardSize - separators) / Core.Model.Board.Size;

            rect.sizeDelta = new Vector2(boardSize, boardSize);
            view._cells = new CellView[Core.Model.Board.CellCount];

            for (var row = 0; row < Core.Model.Board.Size; row++)
            for (var col = 0; col < Core.Model.Board.Size; col++)
            {
                var index = row * Core.Model.Board.Size + col;
                var cell = CellView.Create(rect, index, cellSize);

                var x = outer + col * cellSize + SeparatorsBefore(col, thin, thick) + cellSize / 2f;
                var y = outer + row * cellSize + SeparatorsBefore(row, thin, thick) + cellSize / 2f;

                Ui.Place(cell.GetComponent<RectTransform>(),
                    new Vector2(x - boardSize / 2f, boardSize / 2f - y),
                    new Vector2(cellSize, cellSize));

                cell.Tapped += i => view.CellTapped?.Invoke(i);
                view._cells[index] = cell;
            }

            return view;
        }

        static float SeparatorsBefore(int line, float thin, float thick)
        {
            var total = 0f;
            for (var i = 1; i <= line; i++)
                total += i % 3 == 0 ? thick : thin;
            return total;
        }

        readonly int[] _values = new int[Core.Model.Board.CellCount];

        /// <summary>Redraws every cell from the session's current state.</summary>
        public void Render(GameSession session, Puzzle puzzle, int selected, bool showMistakes)
        {
            for (var i = 0; i < Core.Model.Board.CellCount; i++)
                _values[i] = session.ValueAt(i);

            var selectedDigit = selected >= 0 ? _values[selected] : Core.Model.Board.Empty;

            for (var i = 0; i < Core.Model.Board.CellCount; i++)
            {
                var value = session.ValueAt(i);
                var mistake = showMistakes && session.IsMistakeAt(i);

                _cells[i].SetValue(value, puzzle.IsGiven(i), mistake);
                _cells[i].SetNotes(NotesMaskOf(session, i));
                _cells[i].SetHighlight(HighlightFor(i, selected, selectedDigit));
            }
        }

        CellHighlight HighlightFor(int index, int selected, int selectedDigit)
        {
            if (index == selected)
                return CellHighlight.Selected;
            if (selected < 0)
                return CellHighlight.Normal;

            // Highlighting every copy of the selected digit is the single most
            // useful scanning aid in the game.
            if (selectedDigit != Core.Model.Board.Empty && _values[index] == selectedDigit)
                return CellHighlight.SameDigit;

            return IsPeer(index, selected) ? CellHighlight.Peer : CellHighlight.Normal;
        }

        static bool IsPeer(int a, int b)
        {
            foreach (var peer in ConstraintSet.Classic.PeersOf(b))
                if (peer == a)
                    return true;
            return false;
        }

        /// <summary>Pencil marks for a cell, as a 9-bit mask.</summary>
        static int NotesMaskOf(GameSession session, int index)
        {
            var mask = 0;
            for (var digit = 1; digit <= Core.Model.Board.Size; digit++)
                if (session.HasNote(index, digit))
                    mask |= 1 << (digit - 1);
            return mask;
        }
    }
}
