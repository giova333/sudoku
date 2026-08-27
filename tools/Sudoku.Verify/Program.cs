using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Sudoku.Core.Content;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;
using Sudoku.Core.Solving;

namespace Sudoku.Verify
{
    /// <summary>
    /// Checks baked banks the way a player would suffer them: every puzzle must
    /// be uniquely solvable, must agree with its own stored solution, must
    /// grade to the tier it is filed under, and must not repeat.
    ///
    /// This is deliberately exhaustive rather than sampled. Shipping one
    /// unsolvable puzzle is a one-star review, and the check costs a minute.
    /// </summary>
    static class Program
    {
        static int Main(string[] args)
        {
            var dir = args.Length > 0 ? args[0] : "Assets/_Project/Data/Banks";
            if (!Directory.Exists(dir))
            {
                Console.WriteLine($"No bank directory at {Path.GetFullPath(dir)}");
                return 1;
            }

            var files = Directory.GetFiles(dir, "*.bytes");
            Array.Sort(files);

            var watch = Stopwatch.StartNew();
            var failures = 0;
            var totalPuzzles = 0;

            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var problems = new List<string>();
                PuzzleBank bank;

                try
                {
                    bank = PuzzleBankSerializer.Read(File.ReadAllBytes(file));
                }
                catch (PuzzleBankFormatException e)
                {
                    Console.WriteLine($"  {name}: UNREADABLE - {e.Message}");
                    failures++;
                    continue;
                }

                var seen = new HashSet<string>();

                for (var i = 0; i < bank.Count; i++)
                {
                    var puzzle = bank.PuzzleAt(i);
                    var clues = new int[Board.CellCount];
                    var solution = new int[Board.CellCount];
                    for (var c = 0; c < Board.CellCount; c++)
                    {
                        clues[c] = puzzle.ClueAt(c);
                        solution[c] = puzzle.SolutionAt(c);
                    }

                    if (!seen.Add(GridParser.ToText(clues)))
                        problems.Add($"#{i} duplicate");

                    if (SolutionCounter.Count(clues, ConstraintSet.Classic, 2) != 1)
                        problems.Add($"#{i} not uniquely solvable");

                    if (!SolutionCounter.TrySolve(clues, ConstraintSet.Classic, out var actual) ||
                        GridParser.ToText(actual) != GridParser.ToText(solution))
                        problems.Add($"#{i} stored solution disagrees with the puzzle");

                    for (var c = 0; c < Board.CellCount; c++)
                        if (clues[c] != Board.Empty && clues[c] != solution[c])
                        {
                            problems.Add($"#{i} clue at {c} contradicts its solution");
                            break;
                        }

                    if (PuzzleGrader.Grade(clues, ConstraintSet.Classic, DifficultyProfile.Default) != bank.Tier)
                        problems.Add($"#{i} does not grade as {bank.Tier}");

                    if (problems.Count > 5) break;
                }

                totalPuzzles += bank.Count;

                if (problems.Count == 0)
                {
                    Console.WriteLine($"  {name}: OK ({bank.Count} puzzles, tier {bank.Tier})");
                }
                else
                {
                    failures++;
                    Console.WriteLine($"  {name}: {problems.Count} PROBLEM(S)");
                    foreach (var p in problems) Console.WriteLine($"      {p}");
                }
            }

            watch.Stop();
            Console.WriteLine();
            Console.WriteLine($"{files.Length} banks, {totalPuzzles} puzzles checked in {watch.Elapsed.TotalSeconds:F0}s.");
            Console.WriteLine(failures == 0 ? "All banks clean." : $"{failures} bank(s) FAILED.");
            return failures == 0 ? 0 : 1;
        }
    }
}
