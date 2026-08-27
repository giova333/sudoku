namespace Sudoku.Game.Audio
{
    /// <summary>
    /// Every sound the game makes. Eight of them, and no more without a reason:
    /// a puzzle game the player keeps open for twenty minutes earns its sounds
    /// back by being sparing with them.
    ///
    /// The name is also the clip's file name under Resources/Audio, in
    /// lower-kebab-case - see <see cref="AudioService"/>.
    /// </summary>
    public enum Sfx
    {
        /// <summary>A digit landing correctly.</summary>
        Place,

        /// <summary>A cell being cleared, and undo.</summary>
        Erase,

        /// <summary>A wrong digit that cost no heart.</summary>
        Error,

        /// <summary>A hint filling a cell.</summary>
        Hint,

        /// <summary>A 3x3 box finished correctly.</summary>
        BoxComplete,

        /// <summary>The whole grid solved.</summary>
        PuzzleComplete,

        /// <summary>Any button in the interface chrome.</summary>
        ButtonTap,

        /// <summary>A wrong digit that did cost a heart.</summary>
        HeartLost
    }
}
