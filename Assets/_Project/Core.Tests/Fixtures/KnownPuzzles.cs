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

        /// <summary>
        /// The classic solution with four cells blanked: rows 3-4, columns 5
        /// and 8, holding 1/3 and 3/1. Those four cells are an unavoidable set,
        /// so swapping them yields a second valid grid - each row and column
        /// keeps its digits, and each affected box trades a 1 for a 3 at one
        /// cell and a 3 for a 1 at the other. This grid therefore has EXACTLY
        /// two solutions, by construction rather than by computation.
        /// </summary>
        public const string TwoSolutionGrid =
            "534678912" +
            "672195348" +
            "198342567" +
            "859760420" +
            "426850790" +
            "713924856" +
            "961537284" +
            "287419635" +
            "345286179";

        /// <summary>Two 5s in the top row: no solution exists.</summary>
        public const string ContradictoryGrid =
            "550000000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000";

        /// <summary>
        /// "AI Escargot" (Arto Inkala, 2006), published as one of the hardest
        /// Sudoku puzzles ever constructed. Singles alone cannot crack it.
        /// </summary>
        public const string AiEscargot =
            "100007090" +
            "030020008" +
            "009600500" +
            "005300900" +
            "010080002" +
            "600004000" +
            "300000010" +
            "040000007" +
            "007000300";

        /// <summary>An empty grid: 6.67e21 solutions, far past any test limit.</summary>
        public const string EmptyGrid = 
            "000000000000000000000000000000000000000000000000000000000000000000000000000000000";

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
