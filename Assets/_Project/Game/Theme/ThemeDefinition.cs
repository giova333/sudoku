using TMPro;
using UnityEngine;

namespace Sudoku.Game.Theme
{
    /// <summary>
    /// One complete look: every colour the interface can ask for, and the two
    /// typefaces it is set in.
    ///
    /// A ScriptableObject rather than a table in code, because this is the
    /// asset a cosmetics shop would sell. Everything that would otherwise be a
    /// literal colour somewhere in a view lives here, so shipping a new skin is
    /// shipping a file - no build, no code review, no release.
    ///
    /// Every field carries the Light value as its initialiser. That is not
    /// decoration: it means a theme asset that is missing a slot - because it
    /// was authored before the slot existed - reads as Light for that slot
    /// rather than as transparent black, and it gives
    /// <see cref="Themes"/> a working theme to fall back on if the shipped
    /// assets cannot be loaded at all.
    /// </summary>
    [CreateAssetMenu(fileName = "Theme", menuName = "Sudoku/Theme Definition")]
    public sealed class ThemeDefinition : ScriptableObject
    {
        [SerializeField] ThemeChoice _choice = ThemeChoice.Light;

        [Header("Type")]
        [SerializeField] TMP_FontAsset _displayFont;
        [SerializeField] TMP_FontAsset _numeralFont;

        [Header("Surfaces")]
        [SerializeField] Color _screenBackground = new Color(0.96f, 0.96f, 0.94f);
        [SerializeField] Color _boardLine = new Color(0.35f, 0.37f, 0.42f);

        [Header("Cells")]
        [SerializeField] Color _cellBackground = new Color(0.98f, 0.98f, 0.96f);
        [SerializeField] Color _cellPeer = new Color(0.90f, 0.93f, 0.97f);
        [SerializeField] Color _cellSameDigit = new Color(0.80f, 0.87f, 0.96f);
        [SerializeField] Color _cellSelected = new Color(0.65f, 0.80f, 0.98f);
        [SerializeField] Color _cellHintReason = new Color(0.99f, 0.92f, 0.72f);
        [SerializeField] Color _cellHintTarget = new Color(0.99f, 0.76f, 0.32f);

        [Header("Digits")]
        [SerializeField] Color _givenDigit = new Color(0.13f, 0.14f, 0.16f);
        [SerializeField] Color _enteredDigit = new Color(0.16f, 0.38f, 0.72f);
        [SerializeField] Color _errorDigit = new Color(0.83f, 0.21f, 0.24f);
        [SerializeField] Color _noteDigit = new Color(0.45f, 0.47f, 0.52f);

        [Header("Text")]
        [SerializeField] Color _title = new Color(0.16f, 0.17f, 0.20f);
        [SerializeField] Color _body = new Color(0.16f, 0.17f, 0.20f);
        [SerializeField] Color _muted = new Color(0.45f, 0.47f, 0.52f);
        [SerializeField] Color _disabled = new Color(0.62f, 0.64f, 0.68f);
        [SerializeField] Color _danger = new Color(0.83f, 0.21f, 0.24f);
        [SerializeField] Color _celebrate = new Color(0.15f, 0.55f, 0.30f);

        [Header("Controls")]
        [SerializeField] Color _buttonFill = new Color(0.93f, 0.94f, 0.96f);
        [SerializeField] Color _buttonText = new Color(0.16f, 0.17f, 0.20f);
        [SerializeField] Color _primaryFill = new Color(0.55f, 0.75f, 0.98f);
        [SerializeField] Color _primaryText = new Color(0.16f, 0.17f, 0.20f);
        [SerializeField] Color _warnFill = new Color(0.97f, 0.80f, 0.55f);
        [SerializeField] Color _toggleOn = new Color(0.55f, 0.75f, 0.98f);
        [SerializeField] Color _toggleOff = new Color(0.87f, 0.88f, 0.90f);

        [Header("Numpad")]
        [SerializeField] Color _numpadFill = new Color(0.93f, 0.94f, 0.96f);
        [SerializeField] Color _numpadDisabled = new Color(0.86f, 0.87f, 0.89f);
        [SerializeField] Color _numpadActive = new Color(0.55f, 0.75f, 0.98f);
        [SerializeField] Color _numpadLabel = new Color(0.16f, 0.17f, 0.20f);
        [SerializeField] Color _numpadLabelExhausted = new Color(0.16f, 0.17f, 0.20f, 0.35f);
        [SerializeField] Color _numpadBadge = new Color(0.50f, 0.52f, 0.57f);
        [SerializeField] Color _numpadHintPending = new Color(0.99f, 0.76f, 0.32f);

        /// <summary>Which shipped look this asset is, so the theme service can find it
        /// by the value the preference persists rather than by file name.</summary>
        public ThemeChoice Choice => _choice;

        /// <summary>Fredoka: headings, buttons and the numpad. Null until the font
        /// assets have been generated - see the editor's Sudoku/Theme menu.</summary>
        public TMP_FontAsset DisplayFont => _displayFont;

        /// <summary>Nunito: board digits and pencil marks, chosen for numerals that stay
        /// unambiguous at note size.</summary>
        public TMP_FontAsset NumeralFont => _numeralFont;

        /// <summary>
        /// What a role looks like in this theme. A switch rather than an array
        /// indexed by the slot, so inserting a slot cannot silently shift every
        /// colour in every shipped asset by one.
        /// </summary>
        public Color Of(ThemeSlot slot)
        {
            switch (slot)
            {
                case ThemeSlot.ScreenBackground: return _screenBackground;
                case ThemeSlot.BoardLine: return _boardLine;

                case ThemeSlot.CellBackground: return _cellBackground;
                case ThemeSlot.CellPeer: return _cellPeer;
                case ThemeSlot.CellSameDigit: return _cellSameDigit;
                case ThemeSlot.CellSelected: return _cellSelected;
                case ThemeSlot.CellHintReason: return _cellHintReason;
                case ThemeSlot.CellHintTarget: return _cellHintTarget;

                case ThemeSlot.GivenDigit: return _givenDigit;
                case ThemeSlot.EnteredDigit: return _enteredDigit;
                case ThemeSlot.ErrorDigit: return _errorDigit;
                case ThemeSlot.NoteDigit: return _noteDigit;

                case ThemeSlot.Title: return _title;
                case ThemeSlot.Body: return _body;
                case ThemeSlot.Muted: return _muted;
                case ThemeSlot.Disabled: return _disabled;
                case ThemeSlot.Danger: return _danger;
                case ThemeSlot.Celebrate: return _celebrate;

                case ThemeSlot.ButtonFill: return _buttonFill;
                case ThemeSlot.ButtonText: return _buttonText;
                case ThemeSlot.PrimaryFill: return _primaryFill;
                case ThemeSlot.PrimaryText: return _primaryText;
                case ThemeSlot.WarnFill: return _warnFill;
                case ThemeSlot.ToggleOn: return _toggleOn;
                case ThemeSlot.ToggleOff: return _toggleOff;

                case ThemeSlot.NumpadFill: return _numpadFill;
                case ThemeSlot.NumpadDisabled: return _numpadDisabled;
                case ThemeSlot.NumpadActive: return _numpadActive;
                case ThemeSlot.NumpadLabel: return _numpadLabel;
                case ThemeSlot.NumpadLabelExhausted: return _numpadLabelExhausted;
                case ThemeSlot.NumpadBadge: return _numpadBadge;
                case ThemeSlot.NumpadHintPending: return _numpadHintPending;

                // A slot this asset predates. Magenta would be a designer's
                // signal, but this is a shipping player's screen - body text is
                // wrong quietly rather than loudly.
                default: return _body;
            }
        }

        /// <summary>
        /// Assigns the generated font assets. Called by the editor command that
        /// bakes them, so generating the atlases also wires them into every
        /// shipped theme - the alternative is two themes and a checklist.
        /// </summary>
        public void SetFonts(TMP_FontAsset display, TMP_FontAsset numerals)
        {
            _displayFont = display;
            _numeralFont = numerals;
        }
    }
}
