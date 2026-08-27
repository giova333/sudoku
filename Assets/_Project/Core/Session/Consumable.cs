namespace Sudoku.Core.Session
{
    /// <summary>
    /// The two things a puzzle can run out of. They are one enum rather than two
    /// interfaces because everything that will ever sell them - a rewarded ad, a
    /// bundle, a subscription that removes the limit - treats them as the same
    /// kind of thing with a different label.
    /// </summary>
    public enum Consumable
    {
        Heart = 0,
        Hint = 1
    }
}
