using Sudoku.Core.Model;
using Sudoku.Game.Bootstrap;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sudoku.Game.Board
{
    /// <summary>How a cell should read to the player right now.</summary>
    public enum CellHighlight
    {
        Normal,
        Peer,
        SameDigit,
        Selected
    }

    /// <summary>
    /// One of the 81 cells: a background whose colour carries selection and
    /// peer highlighting, a digit, and a 3x3 block of pencil marks.
    /// </summary>
    public sealed class CellView : MonoBehaviour, IPointerClickHandler
    {
        static readonly Color Background = new Color(0.98f, 0.98f, 0.96f);
        static readonly Color PeerBackground = new Color(0.90f, 0.93f, 0.97f);
        static readonly Color SameDigitBackground = new Color(0.80f, 0.87f, 0.96f);
        static readonly Color SelectedBackground = new Color(0.65f, 0.80f, 0.98f);

        static readonly Color GivenText = new Color(0.13f, 0.14f, 0.16f);
        static readonly Color EnteredText = new Color(0.16f, 0.38f, 0.72f);
        static readonly Color ErrorText = new Color(0.83f, 0.21f, 0.24f);
        static readonly Color NoteText = new Color(0.45f, 0.47f, 0.52f);

        Image _background;
        Text _value;
        readonly Text[] _notes = new Text[10];
        Image _errorUnderline;

        public int Index { get; private set; }

        /// <summary>Raised when the player taps this cell.</summary>
        public System.Action<int> Tapped;

        public static CellView Create(Transform parent, int index, float size)
        {
            var rect = Ui.Rect($"Cell{index}", parent);
            rect.sizeDelta = new Vector2(size, size);

            var view = rect.gameObject.AddComponent<CellView>();
            view.Index = index;

            view._background = rect.gameObject.AddComponent<Image>();
            view._background.color = Background;
            view._background.raycastTarget = true;

            // Nine separate labels in a 3x3 block, rather than one multi-line
            // string: a proportional font would not line the columns up, and
            // scanning notes is most of what playing Sudoku actually is.
            var noteSize = size / 3f;
            for (var digit = 1; digit <= 9; digit++)
            {
                var label = Ui.Label($"Note{digit}", rect, Mathf.RoundToInt(size * 0.22f), NoteText);
                var col = (digit - 1) % 3;
                var row = (digit - 1) / 3;
                Ui.Place(label.rectTransform,
                    new Vector2((col - 1) * noteSize, (1 - row) * noteSize),
                    new Vector2(noteSize, noteSize));
                label.text = "";
                view._notes[digit] = label;
            }

            view._value = Ui.Label("Value", rect, Mathf.RoundToInt(size * 0.60f), GivenText);
            Ui.Stretch(view._value.rectTransform);

            // A non-colour signal for errors, so the board still works for a
            // player who cannot separate the red from the blue.
            view._errorUnderline = Ui.Panel("ErrorUnderline", rect, ErrorText);
            var underline = view._errorUnderline.rectTransform;
            underline.anchorMin = new Vector2(0.25f, 0f);
            underline.anchorMax = new Vector2(0.75f, 0f);
            underline.offsetMin = new Vector2(0, size * 0.12f);
            underline.offsetMax = new Vector2(0, size * 0.12f + 2f);
            view._errorUnderline.enabled = false;

            return view;
        }

        public void SetHighlight(CellHighlight highlight)
        {
            switch (highlight)
            {
                case CellHighlight.Selected: _background.color = SelectedBackground; break;
                case CellHighlight.SameDigit: _background.color = SameDigitBackground; break;
                case CellHighlight.Peer: _background.color = PeerBackground; break;
                default: _background.color = Background; break;
            }
        }

        public void SetValue(int digit, bool isGiven, bool isMistake)
        {
            var filled = digit != Core.Model.Board.Empty;
            _value.text = filled ? digit.ToString() : "";
            for (var d = 1; d <= 9; d++)
                _notes[d].enabled = !filled;
            _value.color = isMistake ? ErrorText : isGiven ? GivenText : EnteredText;
            _value.fontStyle = isGiven ? FontStyle.Bold : FontStyle.Normal;
            _errorUnderline.enabled = isMistake;
        }

        /// <summary>Shows pencil marks in their fixed 3x3 positions.</summary>
        public void SetNotes(int mask)
        {
            for (var digit = 1; digit <= 9; digit++)
                _notes[digit].text = (mask & (1 << (digit - 1))) != 0 ? digit.ToString() : "";
        }

        public void OnPointerClick(PointerEventData eventData) => Tapped?.Invoke(Index);
    }
}
