namespace Sudoku.Core.Tests.Fixtures
{
    /// <summary>
    /// Hand-verified puzzles used as an independent source of truth for tests.
    /// Values here are transcribed from published puzzles and their published
    /// solutions - they are never computed by the code under test.
    /// </summary>
    public static class KnownPuzzles
    {
        /// <summary>
        /// The canonical Sudoku example (Wikipedia, "Sudoku"). 30 clues,
        /// uniquely solvable, solvable by singles alone.
        /// </summary>
        public const string ClassicClues =
            "530070000" +
            "600195000" +
            "098000060" +
            "800060003" +
            "400803001" +
            "700020006" +
            "060000280" +
            "000419005" +
            "000080079";

        public const string ClassicSolution =
            "534678912" +
            "672195348" +
            "198342567" +
            "859761423" +
            "426853791" +
            "713924856" +
            "961537284" +
            "287419635" +
            "345286179";
    }
}
