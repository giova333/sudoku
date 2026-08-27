using Sudoku.Core.Model;
using Sudoku.Core.Solving;

namespace Sudoku.Core.Difficulty
{
    /// <summary>
    /// Grades by the hardest technique a puzzle actually demands - never by
    /// clue count, which is a poor predictor: a 26-clue puzzle can be trivial
    /// and a 32-clue one brutal.
    /// </summary>
    public static class PuzzleGrader
    {
        public static DifficultyTier Grade(int[] clues, ConstraintSet constraints, DifficultyProfile profile)
        {
            var report = TechniqueSolver.Solve(clues, constraints);

            // Anything human technique cannot finish belongs at the top: it is
            // at least as hard as everything we can name.
            if (!report.Solved)
                return DifficultyTier.Master;

            var required = report.HardestTechnique;

            foreach (var rule in profile.Tiers)
                if (required <= rule.HardestAllowed)
                    return rule.Tier;

            return DifficultyTier.Master;
        }
    }
}
