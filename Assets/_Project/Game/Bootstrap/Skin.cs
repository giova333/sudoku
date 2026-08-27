namespace Sudoku.Game.Bootstrap
{
    /// <summary>
    /// The visual language, as five numbers.
    ///
    /// Everything the skin is made of - the fat corner, the thick stroke, the
    /// hard shadow and how far it collapses under a thumb - is a measurement,
    /// not a colour, so it lives in code while the palette lives in a
    /// <see cref="Sudoku.Game.Theme.ThemeDefinition"/>. A skin someone buys
    /// changes the colours; it does not change what a button is shaped like.
    ///
    /// The units are interface units, which the canvas scaler maps one to one
    /// onto the 1080-wide design resolution - so 24 here is 24 px on the
    /// reference phone and proportionally more on a bigger one.
    /// </summary>
    public static class Skin
    {
        /// <summary>The corner every panel, button and card is cut with.</summary>
        public const float CornerRadius = 24f;

        /// <summary>
        /// A board cell's corner. Deliberately much smaller than
        /// <see cref="CornerRadius"/>: at 105 units square with a three-unit
        /// gap around it, a fat corner turns the grid lines into a field of
        /// blobs and the 9x9 stops reading as a grid.
        /// </summary>
        public const float CellCornerRadius = 8f;

        /// <summary>The stroke drawn round every filled shape.</summary>
        public const float BorderWidth = 4f;

        /// <summary>How far a face floats above its shadow at rest.</summary>
        public const float RestingShadow = 6f;

        /// <summary>
        /// And how far it still floats while held. It collapses rather than
        /// disappearing, because a button that lands flat on the page reads as
        /// broken rather than as pressed.
        /// </summary>
        public const float PressedShadow = 2f;
    }
}
