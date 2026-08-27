namespace Sudoku.Game.Theme
{
    /// <summary>
    /// A role a colour plays in the interface, never a colour itself.
    ///
    /// Components name the slot they want and a <see cref="ThemeDefinition"/>
    /// answers what it looks like, which is the whole reason a second theme -
    /// or a bought one - is data rather than a code change. Two slots holding
    /// the same value today are still two slots: the day a skin wants a
    /// primary button that is not the same blue as an on-toggle, the split has
    /// already been made.
    ///
    /// Values are explicit so a theme asset keeps meaning what it meant if a
    /// slot is ever inserted in the middle of this list.
    /// </summary>
    public enum ThemeSlot
    {
        // Surfaces
        ScreenBackground = 0,

        /// <summary>The sheet behind the 9x9 grid. It is what shows through the
        /// gaps between cells, so it is the grid lines as much as the backing.</summary>
        BoardLine = 1,

        // Board cells
        CellBackground = 10,
        CellPeer = 11,
        CellSameDigit = 12,
        CellSelected = 13,

        /// <summary>Amber rather than another blue, so a hint reads as a separate
        /// conversation from ordinary selection and peer scanning.</summary>
        CellHintReason = 14,
        CellHintTarget = 15,

        // Board text
        GivenDigit = 20,
        EnteredDigit = 21,
        ErrorDigit = 22,
        NoteDigit = 23,

        // Type
        Title = 30,
        Body = 31,
        Muted = 32,
        Disabled = 33,
        Danger = 34,
        Celebrate = 35,

        // Controls
        ButtonFill = 40,
        ButtonText = 41,
        PrimaryFill = 42,
        PrimaryText = 43,

        /// <summary>The fill a destructive button takes once it is armed and asking
        /// to be tapped again.</summary>
        WarnFill = 44,
        ToggleOn = 45,
        ToggleOff = 46,

        // Numpad
        NumpadFill = 50,
        NumpadDisabled = 51,
        NumpadActive = 52,
        NumpadLabel = 53,

        /// <summary>A digit with all nine placed. Its own slot rather than the label
        /// colour at a hardcoded alpha, so a theme decides how spent reads.</summary>
        NumpadLabelExhausted = 54,
        NumpadBadge = 55,
        NumpadHintPending = 56
    }
}
