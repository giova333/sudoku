using NUnit.Framework;
using Sudoku.Core.Generation;
using Sudoku.Core.Model;
using Sudoku.Core.Solving;

namespace Sudoku.Core.Tests.Solving
{
    /// <summary>
    /// The one invariant that must hold for every technique, present and
    /// future: a deduction must never contradict the puzzle's real solution.
    ///
    /// A placement must name the solution's digit, and an elimination must
    /// never strike the solution's digit. The solution comes from the
    /// independently tested <see cref="SolutionCounter"/>, so these assertions
    /// cannot agree with a buggy technique by construction - which is exactly
    /// what makes this the safety net for the harder techniques.
    /// </summary>
    [TestFixture]
    public class TechniqueSoundnessTests
    {
        [Test]
        public void No_technique_ever_contradicts_the_solution()
        {
            for (var seed = 1; seed <= 30; seed++)
            {
                var puzzle = PuzzleGenerator.Generate(seed, ConstraintSet.Classic);
                AssertEveryStepIsSound(puzzle, seed);
            }
        }

        [Test]
        public void No_technique_contradicts_the_solution_on_a_hard_published_puzzle()
        {
            var clues = GridParser.Parse(Fixtures.KnownPuzzles.AiEscargot);
            Assert.That(SolutionCounter.TrySolve(clues, ConstraintSet.Classic, out var solution), Is.True);

            AssertEveryStepIsSound(clues, solution, "AI Escargot");
        }

        static void AssertEveryStepIsSound(Puzzle puzzle, int seed)
        {
            var clues = new int[Board.CellCount];
            var solution = new int[Board.CellCount];
            for (var i = 0; i < Board.CellCount; i++)
            {
                clues[i] = puzzle.ClueAt(i);
                solution[i] = puzzle.SolutionAt(i);
            }
            AssertEveryStepIsSound(clues, solution, $"seed {seed}");
        }

        static void AssertEveryStepIsSound(int[] clues, int[] solution, string label)
        {
            var grid = (int[])clues.Clone();
            var candidates = TechniqueSolver.BuildCandidates(grid, ConstraintSet.Classic);

            for (var guard = 0; guard < 500; guard++)
            {
                var step = TechniqueSolver.NextStepForTesting(grid, candidates, ConstraintSet.Classic);
                if (step == null)
                    return;

                if (step.IsPlacement)
                {
                    Assert.That(step.Digit, Is.EqualTo(solution[step.CellIndex]),
                        $"{label}: {step.Technique} placed {step.Digit} in cell {step.CellIndex}, " +
                        $"but the solution has {solution[step.CellIndex]} there");

                    grid[step.CellIndex] = step.Digit;
                    candidates = TechniqueSolver.BuildCandidates(grid, ConstraintSet.Classic);
                }
                else
                {
                    foreach (var e in step.Eliminations)
                    {
                        Assert.That(e.Digit, Is.Not.EqualTo(solution[e.Cell]),
                            $"{label}: {step.Technique} eliminated {e.Digit} from cell {e.Cell}, " +
                            "but that is the solution's digit");

                        candidates[e.Cell] &= ~(1 << (e.Digit - 1));
                    }
                }
            }

            Assert.Fail($"{label}: technique solver did not settle within 500 steps");
        }
    }
}
