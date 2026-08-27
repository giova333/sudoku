namespace Sudoku.Core.Solving
{
    /// <summary>
    /// Human solving techniques, ordered by how hard they are to spot. The
    /// hardest technique a puzzle requires is what determines its difficulty
    /// tier, so this order is the backbone of grading.
    /// </summary>
    public enum Technique
    {
        NakedSingle = 0,
        HiddenSingle = 1,
        LockedCandidates = 2,
        NakedPair = 3,
        HiddenPair = 4,
        NakedTriple = 5,
        XWing = 6
    }
}
