using System;
using System.Collections.Generic;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Generation;
using Sudoku.Core.Model;

namespace Sudoku.Core.Content
{
    /// <summary>
    /// Produces one tier's bank offline. Puzzles are generated for the target
    /// tier, deduplicated, and returned with a report of what it took.
    ///
    /// This is the whole of the bake; the editor menu item around it only picks
    /// paths and writes files.
    /// </summary>
    public static class BankBaker
    {
        public static BakeResult Bake(BakeRequest request, DifficultyProfile profile, ConstraintSet constraints,
            Action<int, int> onProgress = null)
        {
            var puzzles = new List<Puzzle>(request.Count);
            var seen = new HashSet<string>();

            var attempts = 0;
            var duplicates = 0;
            var seed = request.Seed;

            while (puzzles.Count < request.Count && attempts < request.MaxTotalAttempts)
            {
                attempts++;

                var puzzle = PuzzleGenerator.GenerateForTier(
                    seed++, request.Tier, profile, constraints, request.MaxAttemptsPerPuzzle);

                if (puzzle == null)
                    continue;

                var key = CluesKey(puzzle);
                if (!seen.Add(key))
                {
                    duplicates++;
                    continue;
                }

                puzzles.Add(puzzle);
                onProgress?.Invoke(puzzles.Count, request.Count);
            }

            return new BakeResult(request.Tier, puzzles.ToArray(), request.Count, attempts, duplicates);
        }

        static string CluesKey(Puzzle puzzle)
        {
            var clues = new int[Board.CellCount];
            for (var i = 0; i < Board.CellCount; i++)
                clues[i] = puzzle.ClueAt(i);
            return GridParser.ToText(clues);
        }
    }
}
