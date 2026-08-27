using NUnit.Framework;
using Sudoku.Core.Model;
using Sudoku.Core.Solving;
using Sudoku.Core.Tests.Fixtures;

namespace Sudoku.Core.Tests.Solving
{
    [TestFixture]
    public class SolutionCounterTests
    {
        static int[] Grid(string text) => GridParser.Parse(text);

        [Test]
        public void A_proper_puzzle_has_exactly_one_solution()
        {
            var count = SolutionCounter.Count(Grid(KnownPuzzles.ClassicClues), ConstraintSet.Classic, 2);

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void A_completed_grid_has_exactly_one_solution()
        {
            var count = SolutionCounter.Count(Grid(KnownPuzzles.ClassicSolution), ConstraintSet.Classic, 2);

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void A_grid_containing_an_unavoidable_set_has_two_solutions()
        {
            var count = SolutionCounter.Count(Grid(KnownPuzzles.TwoSolutionGrid), ConstraintSet.Classic, 5);

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void A_contradictory_grid_has_no_solution()
        {
            var count = SolutionCounter.Count(Grid(KnownPuzzles.ContradictoryGrid), ConstraintSet.Classic, 2);

            Assert.That(count, Is.Zero);
        }

        [Test]
        public void Counting_stops_at_the_requested_limit()
        {
            // The empty grid has ~6.7e21 solutions; the counter must early-exit.
            var count = SolutionCounter.Count(Grid(KnownPuzzles.EmptyGrid), ConstraintSet.Classic, 2);

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void A_proper_puzzle_solves_to_its_published_solution()
        {
            var solved = SolutionCounter.TrySolve(Grid(KnownPuzzles.ClassicClues), ConstraintSet.Classic, out var solution);

            Assert.That(solved, Is.True);
            Assert.That(GridParser.ToText(solution), Is.EqualTo(KnownPuzzles.ClassicSolution));
        }

        [Test]
        public void A_contradictory_grid_does_not_solve()
        {
            var solved = SolutionCounter.TrySolve(Grid(KnownPuzzles.ContradictoryGrid), ConstraintSet.Classic, out _);

            Assert.That(solved, Is.False);
        }
    }
}
