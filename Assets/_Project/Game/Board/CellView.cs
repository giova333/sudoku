using Sudoku.Core.Model;
using Sudoku.Game.Bootstrap;
using Sudoku.Game.Theme;
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
        Selected,

        /// <summary>A cell that helps force the hinted one - the "why".</summary>
        HintReason,

        /// <summary>The cell a revealed hint is offering to fill.</summary>
        HintTarget
    }

    /// <summary>
    /// One of the 81 cells: a background whose colour carries selection and
    /// peer highlighting, a digit, and a 3x3 block of pencil marks.
    /// </summary>
    public sealed class CellView : MonoBehaviour, IPointerClickHandler
    {
        Image _background;
        ThemedGraphic _backgroundTheme;
        Text _value;
        ThemedGraphic _valueTheme;
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

            // A cell is a tile, not a box: it gets the skin's rounded corner but
            // neither a stroke nor a shadow. Eighty-one of each would be two
            // hundred and forty-three more graphics for an effect the sheet
            // underneath already gives the grid as a whole.
            view._background = rect.gameObject.AddComponent<Image>();
            view._background.raycastTarget = true;
            Ui.Round(view._background, Skin.CellCornerRadius);
            view._backgroundTheme = ThemedGraphic.Attach(view._background, ThemeSlot.CellBackground);

            // Nine separate labels in a 3x3 block, rather than one multi-line
            // string: a proportional font would not line the columns up, and
            // scanning notes is most of what playing Sudoku actually is.
            var noteSize = size / 3f;
            for (var digit = 1; digit <= 9; digit++)
            {
                var label = Ui.Label($"Note{digit}", rect, Mathf.RoundToInt(size * 0.22f),
                    ThemeSlot.NoteDigit);
                var col = (digit - 1) % 3;
                var row = (digit - 1) / 3;
                Ui.Place(label.rectTransform,
                    new Vector2((col - 1) * noteSize, (1 - row) * noteSize),
                    new Vector2(noteSize, noteSize));
                label.text = "";
                view._notes[digit] = label;
            }

            view._value = Ui.Label("Value", rect, Mathf.RoundToInt(size * 0.60f), ThemeSlot.GivenDigit);
            view._valueTheme = view._value.GetComponent<ThemedGraphic>();
            Ui.Stretch(view._value.rectTransform);

            // A non-colour signal for errors, so the board still works for a
            // player who cannot separate the red from the blue.
            view._errorUnderline = Ui.Rounded("ErrorUnderline", rect, ThemeSlot.ErrorDigit, 3f);
            var underline = view._errorUnderline.rectTransform;
            underline.anchorMin = new Vector2(0.25f, 0f);
            underline.anchorMax = new Vector2(0.75f, 0f);
            underline.offsetMin = new Vector2(0, size * 0.12f);
            underline.offsetMax = new Vector2(0, size * 0.12f + 6f);
            view._errorUnderline.enabled = false;

            return view;
        }

        public void SetHighlight(CellHighlight highlight)
        {
            switch (highlight)
            {
                case CellHighlight.HintTarget: _backgroundTheme.Use(ThemeSlot.CellHintTarget); break;
                case CellHighlight.HintReason: _backgroundTheme.Use(ThemeSlot.CellHintReason); break;
                case CellHighlight.Selected: _backgroundTheme.Use(ThemeSlot.CellSelected); break;
                case CellHighlight.SameDigit: _backgroundTheme.Use(ThemeSlot.CellSameDigit); break;
                case CellHighlight.Peer: _backgroundTheme.Use(ThemeSlot.CellPeer); break;
                default: _backgroundTheme.Use(ThemeSlot.CellBackground); break;
            }
        }

        public void SetValue(int digit, bool isGiven, bool isMistake)
        {
            var filled = digit != Core.Model.Board.Empty;
            _value.text = filled ? digit.ToString() : "";
            for (var d = 1; d <= 9; d++)
                _notes[d].enabled = !filled;
            _valueTheme.Use(isMistake ? ThemeSlot.ErrorDigit
                : isGiven ? ThemeSlot.GivenDigit : ThemeSlot.EnteredDigit);
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
