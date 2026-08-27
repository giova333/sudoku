namespace Sudoku.Game.Audio
{
    /// <summary>
    /// How hard the phone should knock. Two strengths only: the whole point of
    /// haptics here is that a mistake feels different from a placement without
    /// the player having to look, and a third shade nobody can name would not
    /// carry any more meaning.
    /// </summary>
    public enum Haptic
    {
        /// <summary>Placement, and anything else that went right.</summary>
        Light,

        /// <summary>A mistake.</summary>
        Firm
    }
}
