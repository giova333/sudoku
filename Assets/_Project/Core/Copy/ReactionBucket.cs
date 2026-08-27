namespace Sudoku.Core.Copy
{
    /// <summary>
    /// The kinds of solve the results card has something to say about.
    ///
    /// The spec names five - fast, slow, perfect, mistake-heavy, hint-heavy -
    /// and most solves are none of them: an ordinary time with one mistake
    /// matches nothing. <see cref="Steady"/> exists so that case gets a written
    /// line rather than a blank card or a joke that does not fit.
    /// </summary>
    public enum ReactionBucket
    {
        /// <summary>The common case: it went fine and there is nothing to point at.</summary>
        Steady = 0,
        Fast = 1,
        Slow = 2,
        Perfect = 3,
        MistakeHeavy = 4,
        HintHeavy = 5
    }
}
