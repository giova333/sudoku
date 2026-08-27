using System;

namespace Sudoku.Core.Solving
{
    /// <summary>
    /// What happened when the technique solver worked a puzzle: whether human
    /// technique was enough, which techniques were needed, and how often. This
    /// is the raw material difficulty grading is computed from.
    /// </summary>
    public sealed class SolveReport
    {
        readonly int[] _counts;

        public SolveReport(bool solved, int[] grid, int[] counts, int totalSteps)
        {
            Solved = solved;
            Grid = grid;
            _counts = counts;
            TotalSteps = totalSteps;
        }

        /// <summary>True when the listed techniques were enough to finish the grid.</summary>
        public bool Solved { get; }

        /// <summary>The board as far as technique could take it.</summary>
        public int[] Grid { get; }

        public int TotalSteps { get; }

        public int CountOf(Technique technique) => _counts[(int)technique];

        /// <summary>
        /// The hardest technique the puzzle actually demanded. This is the
        /// primary input to its difficulty tier.
        /// </summary>
        public Technique HardestTechnique
        {
            get
            {
                var hardest = Technique.NakedSingle;
                foreach (Technique t in Enum.GetValues(typeof(Technique)))
                    if (_counts[(int)t] > 0 && t > hardest)
                        hardest = t;
                return hardest;
            }
        }
    }
}
