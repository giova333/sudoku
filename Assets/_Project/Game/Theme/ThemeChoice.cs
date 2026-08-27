namespace Sudoku.Game.Theme
{
    /// <summary>
    /// Which shipped theme is in force.
    ///
    /// An enum rather than an asset name, because the choice is persisted as a
    /// preference and a preference that stores a file name breaks the day a
    /// file is renamed. A cosmetics pack later adds a member here and an asset
    /// beside the two below - nothing else has to learn about it.
    /// </summary>
    public enum ThemeChoice
    {
        Light = 0,
        Dark = 1
    }
}
